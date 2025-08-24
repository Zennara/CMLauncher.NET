using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class SettingsPage : Page
    {
        private TextBox _steamPathBox = null!;

        public SettingsPage()
        {
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));

            var root = new StackPanel { Margin = new Thickness(20) };
            root.Children.Add(new TextBlock
            {
                Text = "Launcher Settings",
                FontSize = 24,
                Margin = new Thickness(0, 0, 0, 16),
                Foreground = Brushes.White
            });

            // Steam path
            var group = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            group.Children.Add(new TextBlock { Text = "Steam Install Location", Foreground = Brushes.White, FontWeight = FontWeights.SemiBold });
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            _steamPathBox = new TextBox { Width = 420 };
            var guess = SteamLocator.FindGamePath(InstallationService.GetAppId(InstallationService.CMZKey));
            if (!string.IsNullOrWhiteSpace(guess)) _steamPathBox.Text = guess;
            var browse = new Button { Content = "Browse", Margin = new Thickness(8, 0, 0, 0) };
            row.Children.Add(_steamPathBox);
            row.Children.Add(browse);
            group.Children.Add(row);
            root.Children.Add(group);

            Content = root;
        }
    }
}