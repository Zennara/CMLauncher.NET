using System.Windows;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CMLauncher
{
	public partial class LoginWindow : Window
	{
		private bool _rateLimitPopupShown;

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
				string? PromptForGuardCode()
				{
					string? code = null;
					Dispatcher.Invoke(() =>
					{
						var dlg = new SteamGuardCodeWindow { Owner = this };
						if (dlg.ShowDialog() == true)
						{
							code = dlg.Code?.Trim();
						}
					});
					return code;
				}

				void OnRateLimit()
				{
					if (_rateLimitPopupShown) return;
					_rateLimitPopupShown = true;
					Dispatcher.Invoke(() =>
					{
						MessageBox.Show(this, "Steam is rate limiting sign-ins right now. Please wait a minute and try again.", "Rate limited", MessageBoxButton.OK, MessageBoxImage.Warning);
					});
				}

				var result = await Task.Run(() => InstallationService.TryAuthCredentialsWithGuard(u, p, PromptForGuardCode, OnRateLimit));
				if (!result.authOk)
				{
					// If rate limited, keep open; otherwise prompt invalid credentials
					if (_rateLimitPopupShown) return;
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
