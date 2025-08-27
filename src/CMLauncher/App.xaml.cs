using Application = System.Windows.Application;

namespace CMLauncher.NET
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(System.Windows.StartupEventArgs e)
		{
			base.OnStartup(e);

			// Avoid shutting down while no window is open during login/auth
			ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

			// Ask credentials if missing
			if (string.IsNullOrWhiteSpace(CMLauncher.LauncherSettings.Current.SteamUsername) || string.IsNullOrWhiteSpace(CMLauncher.LauncherSettings.Current.SteamPassword))
			{
				while (!PromptLogin()) { }
			}

			// Verify credentials and detect ownership using saved settings
			var (ownsCmz, ownsCmw, authOk) = CMLauncher.InstallationService.TryAuthenticateAndDetectOwnership(
				CMLauncher.LauncherSettings.Current.SteamUsername ?? string.Empty,
				CMLauncher.LauncherSettings.Current.SteamPassword ?? string.Empty);
			if (!authOk)
			{
				// Since there is no cancel, loop until success
				while (true)
				{
					if (!PromptLogin()) continue;
					(ownsCmz, ownsCmw, authOk) = CMLauncher.InstallationService.TryAuthenticateAndDetectOwnership(
						CMLauncher.LauncherSettings.Current.SteamUsername ?? string.Empty,
						CMLauncher.LauncherSettings.Current.SteamPassword ?? string.Empty);
					if (authOk) break;
				}
			}

			CMLauncher.LauncherSettings.Current.OwnsCMZ = ownsCmz;
			CMLauncher.LauncherSettings.Current.OwnsCMW = ownsCmw;
			CMLauncher.LauncherSettings.Current.Save();

			// Ensure main window is created and visible after successful login/auth
			if (Current.MainWindow == null)
			{
				Current.MainWindow = new CMLauncher.MainWindow();
			}
			if (!Current.MainWindow.IsVisible)
			{
				Current.MainWindow.Show();
			}

			// Restore normal shutdown behavior
			ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
		}

		private static bool PromptLogin()
		{
			try
			{
				var win = new CMLauncher.LoginWindow();
				var owner = Current?.MainWindow;
				if (owner != null && owner.IsVisible)
				{
					win.Owner = owner;
					win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
				}
				else
				{
					win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
				}
				return win.ShowDialog() == true;
			}
			catch { return false; }
		}
	}
}
