using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		private static string BuildCredentialArgs(string username, string password)
		{
			return $" -username {username} -password \"{password}\"";
		}

		private static string BuildCredentialArgs()
		{
			var u = LauncherSettings.Current.SteamUsername;
			var p = LauncherSettings.Current.SteamPassword;
			if (!string.IsNullOrWhiteSpace(u) && !string.IsNullOrWhiteSpace(p))
			{
				return BuildCredentialArgs(u, p);
			}
			return string.Empty;
		}

		private static string DetermineGuardFlag(string code) => code.Any(char.IsLetter) ? "-authcode" : "-twofactor";
		private static string BuildCredentialArgs(string username, string password, string? guardCode)
		{
			var args = BuildCredentialArgs(username, password);
			if (!string.IsNullOrWhiteSpace(guardCode))
			{
				var flag = DetermineGuardFlag(guardCode.Trim());
				args += $" {flag} {guardCode.Trim()}";
			}
			return args;
		}
	}
}
