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
			void OnSteamGuard()
			{
				Dispatcher.Invoke(() =>
				{
					MessageBox.Show(this, "Steam Guard confirmation required. Approve the sign-in in your Steam Mobile app. This screen will continue checking.", "Steam Guard", MessageBoxButton.OK, MessageBoxImage.Information);
				});
			}

			var result = await Task.Run(() => InstallationService.TryAuthenticateAndDetectOwnershipDetailed(_username, _password, OnSteamGuard));
			AuthOk = result.authOk;
			OwnsCmz = result.ownsCmz;
			OwnsCmw = result.ownsCmw;
			DialogResult = true;
		}
	}
}
