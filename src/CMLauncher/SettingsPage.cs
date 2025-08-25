using Ookii.Dialogs.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace CMLauncher
{
	public class SettingsPage : Page
	{
		private TextBox _steamPathCMZ = null!;
		private TextBox _steamPathCMW = null!;
		private CheckBox _closeOnLaunch = null!;

		public SettingsPage()
		{
			Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));

			var s = LauncherSettings.Current;

			var root = new StackPanel { Margin = new Thickness(20) };
			root.Children.Add(new TextBlock
			{
				Text = "Launcher Settings",
				FontSize = 24,
				Margin = new Thickness(0, 0, 0, 16),
				Foreground = Brushes.White
			});

			// Close on launch
			_closeOnLaunch = new CheckBox { Content = new TextBlock { Text = "Close launcher after launching the game", Foreground = Brushes.White }, IsChecked = s.CloseOnLaunch };
			_closeOnLaunch.Margin = new Thickness(0, 0, 0, 16);
			_closeOnLaunch.Checked += (_, __) => { LauncherSettings.Current.CloseOnLaunch = true; LauncherSettings.Current.Save(); };
			_closeOnLaunch.Unchecked += (_, __) => { LauncherSettings.Current.CloseOnLaunch = false; LauncherSettings.Current.Save(); };
			root.Children.Add(_closeOnLaunch);

			// CMZ Steam path
			root.Children.Add(new TextBlock { Text = "CastleMiner Z (Steam) Path", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
			var rowZ = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
			_steamPathCMZ = new TextBox { Width = 420, IsReadOnly = true, Text = s.SteamPathCMZ ?? string.Empty };
			if (string.IsNullOrWhiteSpace(_steamPathCMZ.Text))
			{
				var guess = SteamLocator.FindGamePath(InstallationService.GetAppId(InstallationService.CMZKey));
				if (!string.IsNullOrWhiteSpace(guess)) _steamPathCMZ.Text = guess;
			}
			var browseZ = new Button { Content = "Browse", Margin = new Thickness(8, 0, 0, 0) };
			var refreshZ = new Button { Margin = new Thickness(4, 0, 0, 0), Width = 30, Height = 28 };
			refreshZ.Content = new TextBlock { Text = "\uE72C", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
			browseZ.Click += (s2, e2) => BrowseForFolder(_steamPathCMZ, InstallationService.CMZKey);
			refreshZ.Click += (_, __) => { AutoDetectSteamPath(_steamPathCMZ, InstallationService.CMZKey); };
			rowZ.Children.Add(_steamPathCMZ);
			rowZ.Children.Add(browseZ);
			rowZ.Children.Add(refreshZ);
			root.Children.Add(rowZ);

			// CMW Steam path
			root.Children.Add(new TextBlock { Text = "CastleMiner Warfare (Steam) Path", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
			var rowW = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
			_steamPathCMW = new TextBox { Width = 420, IsReadOnly = true, Text = s.SteamPathCMW ?? string.Empty };
			if (string.IsNullOrWhiteSpace(_steamPathCMW.Text))
			{
				var guess = SteamLocator.FindGamePath(InstallationService.GetAppId(InstallationService.CMWKey));
				if (!string.IsNullOrWhiteSpace(guess)) _steamPathCMW.Text = guess;
			}
			var browseW = new Button { Content = "Browse", Margin = new Thickness(8, 0, 0, 0) };
			var refreshW = new Button { Margin = new Thickness(4, 0, 0, 0), Width = 30, Height = 28 };
			refreshW.Content = new TextBlock { Text = "\uE72C", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
			browseW.Click += (s2, e2) => BrowseForFolder(_steamPathCMW, InstallationService.CMWKey);
			refreshW.Click += (_, __) => { AutoDetectSteamPath(_steamPathCMW, InstallationService.CMWKey); };
			rowW.Children.Add(_steamPathCMW);
			rowW.Children.Add(browseW);
			rowW.Children.Add(refreshW);
			root.Children.Add(rowW);

			// Update section
			var updatesRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 12) };
			var checkUpdates = new Button { Content = "Check for Updates", Width = 160, Height = 28 };
			checkUpdates.Click += async (_, __) =>
			{
				var wnd = Window.GetWindow(this);
				await UpdateService.CheckAndPromptAsync(wnd!, silentIfUpToDate: false);
			};
			updatesRow.Children.Add(checkUpdates);
			root.Children.Add(updatesRow);

			Content = root;
		}

		private void BrowseForFolder(TextBox target, string gameKey)
		{
			var dlg = new VistaFolderBrowserDialog
			{
				Description = "Select the Steam game install folder",
				UseDescriptionForTitle = true,
				ShowNewFolderButton = false
			};
			if (dlg.ShowDialog() == true)
			{
				target.Text = dlg.SelectedPath;
				if (string.Equals(gameKey, InstallationService.CMWKey))
					LauncherSettings.Current.SteamPathCMW = dlg.SelectedPath;
				else
					LauncherSettings.Current.SteamPathCMZ = dlg.SelectedPath;
				LauncherSettings.Current.Save();
			}
		}

		private void AutoDetectSteamPath(TextBox target, string gameKey)
		{
			var guess = SteamLocator.FindGamePath(InstallationService.GetAppId(gameKey));
			if (!string.IsNullOrWhiteSpace(guess))
			{
				target.Text = guess;
				if (string.Equals(gameKey, InstallationService.CMWKey))
					LauncherSettings.Current.SteamPathCMW = guess;
				else
					LauncherSettings.Current.SteamPathCMZ = guess;
				LauncherSettings.Current.Save();
			}
		}
	}
}