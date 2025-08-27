using System.Windows;

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
			var u = UsernameBox.Text?.Trim();
			var p = PasswordBox.Password ?? string.Empty;
			LauncherSettings.Current.SteamUsername = string.IsNullOrWhiteSpace(u) ? null : u;
			LauncherSettings.Current.SteamPassword = string.IsNullOrWhiteSpace(p) ? null : p;
			LauncherSettings.Current.Save();
			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}
