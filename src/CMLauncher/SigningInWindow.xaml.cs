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

			string? PromptForGuard()
			{
				string? code = null;
				Dispatcher.Invoke(() =>
				{
					var w = new SteamGuardPromptWindow("A Steam Guard code is required to verify ownership. Enter code:") { Owner = this };
					var ok = w.ShowDialog();
					code = ok == true ? w.GuardCode : null;
				});
				return code;
			}

			void OnRateLimit()
			{
				Dispatcher.Invoke(() =>
				{
					MessageBox.Show(this, "Steam is rate limiting sign-ins right now. Please wait a minute and try again.", "Rate limited", MessageBoxButton.OK, MessageBoxImage.Warning);
				});
			}

			var result = await Task.Run(() => InstallationService.TryAuthenticateAndDetectOwnershipWithGuard(_username, _password, PromptForGuard, OnRateLimit));
			AuthOk = result.authOk;
			OwnsCmz = result.ownsCmz;
			OwnsCmw = result.ownsCmw;
			DialogResult = true;
		}
	}
}
