using System.Windows;

namespace CMLauncher
{
	public partial class SteamGuardCodeWindow : Window
	{
		public string? Code { get; private set; }

		public SteamGuardCodeWindow()
		{
			InitializeComponent();
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			Code = CodeBox.Text?.Trim();
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
