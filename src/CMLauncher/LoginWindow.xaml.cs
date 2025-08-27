using System.Windows;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CMLauncher
{
	public partial class LoginWindow : Window
	{
		private bool _steamGuardPopupShown;

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

			try
			{
				void OnSteamGuard()
				{
					if (_steamGuardPopupShown) return;
					_steamGuardPopupShown = true;
					Dispatcher.Invoke(() =>
					{
						MessageBox.Show(this, "Steam Guard confirmation required. Approve the sign-in in your Steam Mobile app. This window will remain open. Click Login again after approval.", "Steam Guard", MessageBoxButton.OK, MessageBoxImage.Information);
					});
				}

				var result = await Task.Run(() => InstallationService.TryAuthCredentialsWithCallback(u, p, OnSteamGuard));
				if (!result.authOk)
				{
					if (result.steamGuard) return;
					MessageBox.Show(this, "Invalid Steam credentials. Please try again.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				LauncherSettings.Current.SteamUsername = u;
				LauncherSettings.Current.SteamPassword = p;
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
			if (DialogResult != true)
			{
				try { Application.Current.Shutdown(0); } catch { }
				System.Environment.Exit(0);
			}
		}
	}
}
