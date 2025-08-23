using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class SettingsPage : Page
    {
        public SettingsPage()
        {
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));

            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            root.Children.Add(new TextBlock
            {
                Text = "Launcher Settings",
                FontSize = 24,
                Margin = new Thickness(0, 0, 0, 16),
                Foreground = Brushes.White
            });

            var checkbox = new CheckBox
            {
                Content = new TextBlock { Text = "Close launcher after launching the game", Foreground = Brushes.White },
                IsChecked = true,
                Foreground = Brushes.White
            };
            Grid.SetRow(checkbox, 1);
            root.Children.Add(checkbox);

            Content = root;
        }
    }
}