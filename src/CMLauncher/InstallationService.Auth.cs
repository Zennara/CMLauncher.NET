using System;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		public static (bool authOk, bool steamGuard) TryAuthCredentialsWithCallback(string username, string password, Action? onSteamGuardDetected)
		{
			var (output, sg, rl) = RunDepotProbe(CMZAppId, "253431", username, password, onSteamGuardDetected, null);
			if (rl) return (false, sg);
			if (output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
				return (false, sg);
			return (true, sg);
		}

		public static (bool authOk, bool steamGuard) TryAuthCredentialsWithCallback(string username, string password, Action? onSteamGuardDetected, Action? onRateLimitDetected)
		{
			var (output, sg, rl) = RunDepotProbe(CMZAppId, "253431", username, password, onSteamGuardDetected, onRateLimitDetected);
			if (rl) return (false, sg);
			if (output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
				return (false, sg);
			return (true, sg);
		}

		public static (bool authOk, bool steamGuard) TryAuthCredentialsWithGuard(string username, string password, Func<string?> promptForGuardCode, Action? onRateLimitDetected)
		{
			var (output, sg, rl) = RunDepotProbe(CMZAppId, "253431", username, password, () => { }, onRateLimitDetected);
			if (rl) return (false, sg);
			if (sg)
			{
				var code = promptForGuardCode();
				if (string.IsNullOrWhiteSpace(code)) return (false, true);
				var res = RunDepotProbeWithGuard(CMZAppId, "253431", username, password, code);
				output = res.output;
			}
			if (output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
				return (false, sg);
			return (true, sg);
		}
	}
}
