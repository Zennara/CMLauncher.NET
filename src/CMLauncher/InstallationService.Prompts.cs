using System;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		private static bool ContainsSteamGuardPrompt(string text)
		{
			if (string.IsNullOrEmpty(text)) return false;
			return text.Contains("STEAM GUARD", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Steam Guard", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Use the Steam Mobile App to confirm your sign in", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Please enter the 2-factor code", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Two-factor code", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Steam Guard code", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Please enter the auth code", StringComparison.OrdinalIgnoreCase);
		}

		private static bool ContainsRateLimitPrompt(string text)
		{
			if (string.IsNullOrEmpty(text)) return false;
			return text.Contains("RateLimitExceeded", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase);
		}
	}
}
