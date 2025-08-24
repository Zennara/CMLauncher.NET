using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TextBox = System.Windows.Controls.TextBox;
using CheckBox = System.Windows.Controls.CheckBox;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

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
            var refreshZ = new Button { Content = "?", Margin = new Thickness(4, 0, 0, 0), Width = 28 };
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
            var refreshW = new Button { Content = "?", Margin = new Thickness(4, 0, 0, 0), Width = 28 };
            browseW.Click += (s2, e2) => BrowseForFolder(_steamPathCMW, InstallationService.CMWKey);
            refreshW.Click += (_, __) => { AutoDetectSteamPath(_steamPathCMW, InstallationService.CMWKey); };
            rowW.Children.Add(_steamPathCMW);
            rowW.Children.Add(browseW);
            rowW.Children.Add(refreshW);
            root.Children.Add(rowW);

            Content = root;
        }

        private void BrowseForFolder(TextBox target, string gameKey)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = "Select the Steam game install folder";
            dlg.UseDescriptionForTitle = true;
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
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