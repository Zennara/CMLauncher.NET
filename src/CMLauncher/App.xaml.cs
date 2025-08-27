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
			// Ask credentials if missing
			if (string.IsNullOrWhiteSpace(CMLauncher.LauncherSettings.Current.SteamUsername) || string.IsNullOrWhiteSpace(CMLauncher.LauncherSettings.Current.SteamPassword))
			{
				PromptLogin();
			}

			// Verify credentials and detect ownership
			var (ownsCmz, ownsCmw, authOk) = CMLauncher.InstallationService.TryAuthenticateAndDetectOwnership();
			if (!authOk)
			{
				// Prompt to retry until user cancels or success
				while (true)
				{
					if (!PromptLogin()) break;
					(ownsCmz, ownsCmw, authOk) = CMLauncher.InstallationService.TryAuthenticateAndDetectOwnership();
					if (authOk) break;
				}
			}

			CMLauncher.LauncherSettings.Current.OwnsCMZ = ownsCmz;
			CMLauncher.LauncherSettings.Current.OwnsCMW = ownsCmw;
			CMLauncher.LauncherSettings.Current.Save();
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
