using System.Windows;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CMLauncher
{
	public partial class LoginWindow : Window
	{
		public LoginWindow()
		{
			InitializeComponent();
			UsernameBox.Text = LauncherSettings.Current.SteamUsername ?? string.Empty;
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			var u = UsernameBox.Text?.Trim() ?? string.Empty;
			var p = PasswordBox.Password ?? string.Empty;
			if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
			{
				MessageBox.Show(this, "Please enter username and password.", "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// First do a lightweight auth-only probe to keep UI responsive
			try
			{
				bool auth = await Task.Run(() => InstallationService.TryAuthenticateCredentials(u, p));
				if (!auth)
				{
					MessageBox.Show(this, "Invalid Steam credentials. Please try again.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				// Save creds immediately
				LauncherSettings.Current.SteamUsername = u;
				LauncherSettings.Current.SteamPassword = p;
				LauncherSettings.Current.Save();

				// Close dialog to let app start fast
				DialogResult = true;
				Close();

				// After the app starts, do ownership detection in background and persist
				_ = Task.Run(() =>
				{
					var (ownsCmz, ownsCmw, _) = InstallationService.TryAuthenticateAndDetectOwnership(u, p);
					LauncherSettings.Current.OwnsCMZ = ownsCmz;
					LauncherSettings.Current.OwnsCMW = ownsCmw;
					LauncherSettings.Current.Save();
				});
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
