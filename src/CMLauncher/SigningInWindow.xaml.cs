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
			var result = await Task.Run(() => InstallationService.TryAuthenticateAndDetectOwnership(_username, _password));
			AuthOk = result.authOk;
			OwnsCmz = result.ownsCmz;
			OwnsCmw = result.ownsCmw;
			DialogResult = true; // Close dialog and let the caller read properties
		}
	}
}
