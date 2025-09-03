using System;
using System.Threading.Tasks;
using System.Windows;

namespace CMLauncher
{
	public partial class SigningInWindow : Window
	{
		private readonly string _username;
		private readonly string _password;

		public bool AuthOk { get; private set; }
		public bool OwnsCmz { get; private set; }
		public bool OwnsCmw { get; private set; }

		public SigningInWindow(string username, string password)
		{
			_username = username;
			_password = password;
			InitializeComponent();
			Loaded += SigningInWindow_Loaded;
		}

		private async void SigningInWindow_Loaded(object sender, RoutedEventArgs e)
		{
			StatusText.Text = "Checking ownership...";

			void OnLine(string line)
			{
				Dispatcher.Invoke(() => StatusText.Text = line);
			}

			var (ownsCmz, ownsCmw, authOk, accessDenied) = await InstallationService.TryAuthenticateAndDetectOwnershipWithTokenAsync(_username, OnLine);

			if (accessDenied)
			{
				// Clear saved credentials and show login again
				LauncherSettings.Current.SteamUsername = string.Empty;
				LauncherSettings.Current.SteamPassword = string.Empty;
				LauncherSettings.Current.Save();
				MessageBox.Show(this, "Saved login token was rejected. Please sign in again.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
				AuthOk = false;
				DialogResult = false;
				return;
			}

			AuthOk = authOk;
			OwnsCmz = ownsCmz;
			OwnsCmw = ownsCmw;
			DialogResult = true;
		}
	}
}
