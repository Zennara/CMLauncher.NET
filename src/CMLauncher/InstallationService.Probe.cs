using System;
using System.Diagnostics;
using System.Text;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		private static (string output, bool steamGuard, bool rateLimited) RunDepotProbe(string appId, string depotId, string username, string password, Action? onSteamGuardDetected, Action? onRateLimitDetected)
		{
			var ddExe = GetDepotDownloaderExePath();
			if (string.IsNullOrWhiteSpace(ddExe)) return (string.Empty, false, false);

			var creds = BuildCredentialArgs(username, password);
			var psi = new ProcessStartInfo
			{
				FileName = ddExe,
				Arguments = $"-app {appId} -depot {depotId}{creds} -manifest-only",
				WorkingDirectory = System.IO.Path.GetDirectoryName(ddExe) ?? AppDomain.CurrentDomain.BaseDirectory,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			var sb = new StringBuilder();
			bool steamGuard = false;
			bool rateLimited = false;
			using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
			DataReceivedEventHandler onData = (_, e) =>
			{
				if (e.Data == null) return;
				sb.AppendLine(e.Data);
				if (!steamGuard && ContainsSteamGuardPrompt(e.Data))
				{
					steamGuard = true;
					try { onSteamGuardDetected?.Invoke(); } catch { }
				}
				if (!rateLimited && ContainsRateLimitPrompt(e.Data))
				{
					rateLimited = true;
					try { onRateLimitDetected?.Invoke(); } catch { }
				}
			};
			p.OutputDataReceived += onData;
			p.ErrorDataReceived += onData;

			try
			{
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();
			}
			catch { }

			return (sb.ToString(), steamGuard, rateLimited);
		}

		private static (string output, bool steamGuard, bool rateLimited) RunDepotProbeWithGuard(string appId, string depotId, string username, string password, string guardCode)
		{
			var ddExe = GetDepotDownloaderExePath();
			if (string.IsNullOrWhiteSpace(ddExe)) return (string.Empty, false, false);
			var creds = BuildCredentialArgs(username, password, guardCode);
			var psi = new ProcessStartInfo
			{
				FileName = ddExe,
				Arguments = $"-app {appId} -depot {depotId}{creds} -manifest-only",
				WorkingDirectory = System.IO.Path.GetDirectoryName(ddExe) ?? AppDomain.CurrentDomain.BaseDirectory,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			var sb = new StringBuilder();
			bool steamGuard = false;
			bool rateLimited = false;
			using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
			DataReceivedEventHandler onData = (_, e) =>
			{
				if (e.Data == null) return;
				sb.AppendLine(e.Data);
				if (!steamGuard && ContainsSteamGuardPrompt(e.Data)) steamGuard = true;
				if (!rateLimited && ContainsRateLimitPrompt(e.Data)) rateLimited = true;
			};
			p.OutputDataReceived += onData;
			p.ErrorDataReceived += onData;
			try
			{
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();
			}
			catch { }
			return (sb.ToString(), steamGuard, rateLimited);
		}
	}
}
