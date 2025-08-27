using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		private class SteamInfoFile
		{
			public string? timestamp { get; set; }
		}

		private static void HandleAuthFailureDuringOperation()
		{
			try
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					LauncherSettings.Current.SteamUsername = null;
					LauncherSettings.Current.SteamPassword = null;
					LauncherSettings.Current.OwnsCMZ = null;
					LauncherSettings.Current.OwnsCMW = null;
					LauncherSettings.Current.Save();

					try
					{
						var login = new LoginWindow();
						var ok = login.ShowDialog();
						if (ok != true)
						{
							Application.Current.Shutdown();
						}
					}
					catch { }
				});
			}
			catch { }
		}
	}
}
