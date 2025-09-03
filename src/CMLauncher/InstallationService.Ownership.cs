using System;
using System.Threading.Tasks;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		public static (bool ownsCmz, bool ownsCmw, bool authOk, bool steamGuard) TryAuthenticateAndDetectOwnershipDetailed(string username, string password, Action? onSteamGuardDetected, Action? onRateLimitDetected)
		{
			bool ownsCmz = false, ownsCmw = false, authOk = false, steamGuard = false;
			var ddExe = GetDepotDownloaderExePath();
			if (string.IsNullOrWhiteSpace(ddExe)) return (false, false, false, false);

			(string app, string depot)[] checks = new[] { (CMZAppId, "253431"), (CMWAppId, "675211") };
			foreach (var (app, depot) in checks)
			{
				try
				{
					var res = RunDepotProbe(app, depot, username, password, onSteamGuardDetected, onRateLimitDetected);
					var output = res.output;
					var sg = res.steamGuard;
					var rl = res.rateLimited;
					if (sg) steamGuard = true;
					if (rl) return (false, false, false, steamGuard);
					if (output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
					{
						return (false, false, false, steamGuard);
					}
					if (output.Contains("Got depot key", StringComparison.OrdinalIgnoreCase) || output.Contains("Processing depot", StringComparison.OrdinalIgnoreCase))
					{
						authOk = true;
						if (app == CMZAppId) ownsCmz = true; else ownsCmw = true;
					}
					else if (output.Contains("is not available from this account", StringComparison.OrdinalIgnoreCase))
					{
						authOk = true; // Auth worked but not owned
					}
				}
				catch { }
			}
			return (ownsCmz, ownsCmw, authOk, steamGuard);
		}

		public static (bool ownsCmz, bool ownsCmw, bool authOk, bool steamGuard) TryAuthenticateAndDetectOwnershipWithGuard(string username, string password, Func<string?> promptForGuardCode, Action? onRateLimitDetected)
		{
			bool ownsCmz = false, ownsCmw = false, authOk = false, steamGuard = false;
			var ddExe = GetDepotDownloaderExePath();
			if (string.IsNullOrWhiteSpace(ddExe)) return (false, false, false, false);

			string? guardCode = null;
			(string app, string depot)[] checks = new[] { (CMZAppId, "253431"), (CMWAppId, "675211") };
			foreach (var (app, depot) in checks)
			{
				try
				{
					(string output, bool sg, bool rl) res;
					if (string.IsNullOrWhiteSpace(guardCode))
					{
						var probe = RunDepotProbe(app, depot, username, password, () => { steamGuard = true; }, onRateLimitDetected);
						res = (probe.output, probe.steamGuard, probe.rateLimited);
						if (res.rl) return (false, false, false, steamGuard);
						if (res.sg)
						{
							steamGuard = true;
							guardCode = promptForGuardCode();
							if (string.IsNullOrWhiteSpace(guardCode)) return (false, false, false, steamGuard);
							var withGuard = RunDepotProbeWithGuard(app, depot, username, password, guardCode);
							res = (withGuard.output, withGuard.steamGuard, withGuard.rateLimited);
							if (res.rl) return (false, false, false, steamGuard);
						}
					}
					else
					{
						var withGuard = RunDepotProbeWithGuard(app, depot, username, password, guardCode);
						res = (withGuard.output, withGuard.steamGuard, withGuard.rateLimited);
						if (res.rl) return (false, false, false, steamGuard);
					}

					var output = res.output;
					if (output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
					{
						return (false, false, false, steamGuard);
					}
					if (output.Contains("Got depot key", StringComparison.OrdinalIgnoreCase) || output.Contains("Processing depot", StringComparison.OrdinalIgnoreCase))
					{
						authOk = true;
						if (app == CMZAppId) ownsCmz = true; else ownsCmw = true;
					}
					else if (output.Contains("is not available from this account", StringComparison.OrdinalIgnoreCase))
					{
						authOk = true; // Auth worked but not owned
					}
				}
				catch { }
			}
			return (ownsCmz, ownsCmw, authOk, steamGuard);
		}

		public static (bool ownsCmz, bool ownsCmw, bool authOk, bool steamGuard) TryAuthenticateAndDetectOwnershipDetailed(string username, string password, Action? onSteamGuardDetected)
			=> TryAuthenticateAndDetectOwnershipDetailed(username, password, onSteamGuardDetected, null);


		public static async Task<(bool ownsCmz, bool ownsCmw, bool authOk, bool accessDenied)> TryAuthenticateAndDetectOwnershipWithTokenAsync(string username, Action<string>? onLine = null)
		{
			bool ownsCmz = false, ownsCmw = false, authOk = false, accessDenied = false;
			var ddExe = GetDepotDownloaderExePath();
			if (string.IsNullOrWhiteSpace(ddExe)) return (false, false, false, false);

			async Task<(bool owned, bool ok, bool denied)> ProbeAsync(string appId, string depotId)
			{
				bool owned = false, ok = false, denied = false;
				var launcher = new DepotDownloaderWrapper(ddExe!);
				launcher.OutputReceived += line =>
				{
					onLine?.Invoke(line);
					var lower = line.ToLowerInvariant();
					if (!denied && lower.Contains("access token was rejected") && lower.Contains("accessdenied"))
					{
						denied = true;
					}
					if (!owned && (lower.Contains("got depot key") || lower.Contains("processing depot")))
					{
						owned = true;
						ok = true;
					}
					if (!ok && (lower.Contains("is not available from this account") || lower.Contains("does not own") || lower.Contains("not available from this account")))
					{
						ok = true; // authenticated but not owned
					}
					if (!ok && (lower.Contains("got appinfo") || lower.Contains("using app branch") || lower.Contains("got 1 licenses") || lower.Contains("licenses for account")))
					{
						ok = true; // authenticated, will determine owned separately
					}
				};

				var args = $"-app {appId} -depot {depotId} -manifest-only -username {username} -remember-password";
				var exit = await launcher.RunAsync(args).ConfigureAwait(false);
				// If exit is 0 and ok not set but we saw no denied, still treat as ok
				if (!ok && exit == 0 && !denied) ok = true;
				return (owned, ok, denied);
			}

			// CMZ probe
			var cmz = await ProbeAsync("253430", "253431");
			ownsCmz = cmz.owned; authOk = cmz.ok; accessDenied = cmz.denied;
			if (!accessDenied)
			{
				// CMW probe (best effort if IDs are available). If unknown, ignore gracefully.
				try
				{
					var cmw = await ProbeAsync("675210", "675211");
					ownsCmw = cmw.owned; authOk = authOk || cmw.ok; accessDenied = accessDenied || cmw.denied;
				}
				catch { }
			}

			return (ownsCmz, ownsCmw, authOk, accessDenied);
		}
	}
}
