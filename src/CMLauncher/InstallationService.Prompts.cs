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
				|| text.Contains("Please enter the auth code", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("{INPUT_HERE}", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("auth code sent to", StringComparison.OrdinalIgnoreCase);
		}

		private static bool ContainsRateLimitPrompt(string text)
		{
			if (string.IsNullOrEmpty(text)) return false;
			return text.Contains("RateLimitExceeded", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase);
		}

		private static bool ContainsInvalidGuardPrompt(string text)
		{
			if (string.IsNullOrEmpty(text)) return false;
			return text.Contains("previous 2-factor auth code", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("2-factor auth code you have provided is incorrect", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("auth code", StringComparison.OrdinalIgnoreCase) && text.Contains("incorrect", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("invalid auth code", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("invalid two-factor", StringComparison.OrdinalIgnoreCase);
		}
	}
}
