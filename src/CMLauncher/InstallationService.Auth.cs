using System;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		public static (bool authOk, bool steamGuard) TryAuthCredentialsWithCallback(string username, string password, Action? onSteamGuardDetected)
		{
			var res = RunDepotProbe(CMZAppId, "253431", username, password, onSteamGuardDetected, null);
			if (res.rateLimited) return (false, res.steamGuard);
			if (res.output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || res.output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
				return (false, res.steamGuard);
			return (true, res.steamGuard);
		}

		public static (bool authOk, bool steamGuard) TryAuthCredentialsWithCallback(string username, string password, Action? onSteamGuardDetected, Action? onRateLimitDetected)
		{
			var res = RunDepotProbe(CMZAppId, "253431", username, password, onSteamGuardDetected, onRateLimitDetected);
			if (res.rateLimited) return (false, res.steamGuard);
			if (res.output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || res.output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
				return (false, res.steamGuard);
			return (true, res.steamGuard);
		}

		public static (bool authOk, bool steamGuard) TryAuthCredentialsWithGuard(string username, string password, Func<string?> promptForGuardCode, Action? onRateLimitDetected)
		{
			// Simple approach: just show the external console window and let user handle everything manually
			OpenDepotDownloaderConsole(CMZAppId, "253431", username, password);
			
			// Also try to detect Steam Guard and show popup as backup
			var res = RunDepotProbe(CMZAppId, "253431", username, password, () => 
			{
				// Steam Guard detected, show popup
				try
				{
					System.Windows.Application.Current?.Dispatcher.Invoke(() =>
					{
						var code = promptForGuardCode();
						// We can't easily send this to the already-running console, so just let user handle it manually
					});
				}
				catch { }
			}, onRateLimitDetected);

			if (res.rateLimited) return (false, res.steamGuard);
			if (res.steamGuard)
			{
				// Keep asking for codes until success
				while (true)
				{
					var code = promptForGuardCode();
					if (string.IsNullOrWhiteSpace(code)) return (false, true);
					
					var withGuardRes = RunDepotProbeWithGuard(CMZAppId, "253431", username, password, code);
					if (withGuardRes.output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || 
						ContainsInvalidGuardPrompt(withGuardRes.output))
					{
						continue; // wrong code, try again
					}
					return (true, true);
				}
			}

			if (res.output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || res.output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
				return (false, res.steamGuard);
			return (true, res.steamGuard);
		}
	}
}
