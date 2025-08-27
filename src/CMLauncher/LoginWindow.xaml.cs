using System.Windows;
using System.ComponentModel;

namespace CMLauncher
{
	public partial class LoginWindow : Window
	{
		public LoginWindow()
		{
			InitializeComponent();
			UsernameBox.Text = LauncherSettings.Current.SteamUsername ?? string.Empty;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			var u = UsernameBox.Text?.Trim() ?? string.Empty;
			var p = PasswordBox.Password ?? string.Empty;
			if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
			{
				MessageBox.Show(this, "Please enter username and password.", "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Validate credentials by running DepotDownloader -manifest-only
			try
			{
				var (ownsCmz, ownsCmw, authOk) = InstallationService.TryAuthenticateAndDetectOwnership(u, p);
				if (!authOk)
				{
					MessageBox.Show(this, "Invalid Steam credentials. Please try again.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Error);
					return; // keep dialog open
				}

				// Save creds and ownership flags when authentication succeeded
				LauncherSettings.Current.SteamUsername = u;
				LauncherSettings.Current.SteamPassword = p;
				LauncherSettings.Current.OwnsCMZ = ownsCmz;
				LauncherSettings.Current.OwnsCMW = ownsCmw;
				LauncherSettings.Current.Save();

				DialogResult = true;
				Close();
			}
			catch
			{
				MessageBox.Show(this, "Failed to validate credentials. Ensure DepotDownloader is available.", "Login error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			// If the dialog wasn't accepted (no successful login), exit the app
			if (DialogResult != true)
			{
				try { Application.Current.Shutdown(0); } catch { }
				// Hard-exit as a fallback in case a message pump is still running
				System.Environment.Exit(0);
			}
		}
	}
}
