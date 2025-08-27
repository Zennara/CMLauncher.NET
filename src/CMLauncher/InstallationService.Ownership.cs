using System;

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
	}
}
