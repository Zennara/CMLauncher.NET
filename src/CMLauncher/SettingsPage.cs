using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            root.Children.Add(_closeOnLaunch);

            // CMZ Steam path
            root.Children.Add(new TextBlock { Text = "CastleMiner Z (Steam) Path", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
            var rowZ = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
            _steamPathCMZ = new TextBox { Width = 420, Text = s.SteamPathCMZ ?? string.Empty };
            if (string.IsNullOrWhiteSpace(_steamPathCMZ.Text))
            {
                var guess = SteamLocator.FindGamePath(InstallationService.GetAppId(InstallationService.CMZKey));
                if (!string.IsNullOrWhiteSpace(guess)) _steamPathCMZ.Text = guess;
            }
            var browseZ = new Button { Content = "Browse", Margin = new Thickness(8, 0, 0, 0) };
            browseZ.Click += (s2, e2) => BrowseForFolder(_steamPathCMZ);
            rowZ.Children.Add(_steamPathCMZ);
            rowZ.Children.Add(browseZ);
            root.Children.Add(rowZ);

            // CMW Steam path
            root.Children.Add(new TextBlock { Text = "CastleMiner Warfare (Steam) Path", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
            var rowW = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
            _steamPathCMW = new TextBox { Width = 420, Text = s.SteamPathCMW ?? string.Empty };
            if (string.IsNullOrWhiteSpace(_steamPathCMW.Text))
            {
                var guess = SteamLocator.FindGamePath(InstallationService.GetAppId(InstallationService.CMWKey));
                if (!string.IsNullOrWhiteSpace(guess)) _steamPathCMW.Text = guess;
            }
            var browseW = new Button { Content = "Browse", Margin = new Thickness(8, 0, 0, 0) };
            browseW.Click += (s2, e2) => BrowseForFolder(_steamPathCMW);
            rowW.Children.Add(_steamPathCMW);
            rowW.Children.Add(browseW);
            root.Children.Add(rowW);

            // Save button
            var save = new Button { Content = "Save", Padding = new Thickness(10, 6, 10, 6) };
            save.Click += (s3, e3) =>
            {
                var cfg = LauncherSettings.Current;
                cfg.CloseOnLaunch = _closeOnLaunch.IsChecked == true;
                cfg.SteamPathCMZ = string.IsNullOrWhiteSpace(_steamPathCMZ.Text) ? null : _steamPathCMZ.Text.Trim();
                cfg.SteamPathCMW = string.IsNullOrWhiteSpace(_steamPathCMW.Text) ? null : _steamPathCMW.Text.Trim();
                cfg.Save();
                MessageBox.Show("Settings saved.");
            };
            root.Children.Add(save);

            Content = root;
        }

        private void BrowseForFolder(TextBox target)
        {
            // Fallback folder "picker": ask user to pick an executable inside the folder
            var dlg = new OpenFileDialog
            {
                Title = "Pick any file inside the desired folder",
                Filter = "All files|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                var dir = System.IO.Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(dir))
                {
                    target.Text = dir;
                }
            }
        }
    }
}