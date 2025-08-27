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

			// Ask credentials if missing and block until provided
			if (string.IsNullOrWhiteSpace(CMLauncher.LauncherSettings.Current.SteamUsername) || string.IsNullOrWhiteSpace(CMLauncher.LauncherSettings.Current.SteamPassword))
			{
				while (!PromptLogin()) { }
			}

			// Show signing-in window (modal) to check ownership
			var user = CMLauncher.LauncherSettings.Current.SteamUsername ?? string.Empty;
			var pass = CMLauncher.LauncherSettings.Current.SteamPassword ?? string.Empty;
			var signing = new CMLauncher.SigningInWindow(user, pass) { ShowInTaskbar = false };
			bool? okDialog = signing.ShowDialog();
			bool authOk = okDialog == true && signing.AuthOk;
			bool ownsCmz = signing.OwnsCmz;
			bool ownsCmw = signing.OwnsCmw;

			if (!authOk)
			{
				// Ownership/auth failed: re-run login until success
				while (true)
				{
					if (!PromptLogin()) continue;
					user = CMLauncher.LauncherSettings.Current.SteamUsername ?? string.Empty;
					pass = CMLauncher.LauncherSettings.Current.SteamPassword ?? string.Empty;
					signing = new CMLauncher.SigningInWindow(user, pass) { ShowInTaskbar = false };
					okDialog = signing.ShowDialog();
					authOk = okDialog == true && signing.AuthOk;
					ownsCmz = signing.OwnsCmz;
					ownsCmw = signing.OwnsCmw;
					if (authOk) break;
				}
			}

			CMLauncher.LauncherSettings.Current.OwnsCMZ = ownsCmz;
			CMLauncher.LauncherSettings.Current.OwnsCMW = ownsCmw;
			CMLauncher.LauncherSettings.Current.Save();

			// Create and show the real main window now; explicitly assign as MainWindow
			Current.MainWindow = new CMLauncher.MainWindow();
			Current.MainWindow.Show();

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
