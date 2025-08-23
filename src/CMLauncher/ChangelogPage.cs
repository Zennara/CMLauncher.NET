using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class ChangelogPage : Page
    {
        public ChangelogPage()
        {
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));

            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            root.Children.Add(new TextBlock
            {
                Text = "What's New",
                FontSize = 24,
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = Brushes.White
            });

            var list = new StackPanel();
            Grid.SetRow(list, 1);

            AddEntry(list, "v1.0.0", new[]
            {
                "Initial release of CM Launcher",
                "Added support for CMZ and CMW",
                "Basic settings and configuration"
            });

            AddEntry(list, "v0.9.5-beta", new[]
            {
                "Beta testing version",
                "Fixed multiple bugs in the launcher interface",
                "Improved mod loading performance"
            });

            root.Children.Add(list);
            Content = root;
        }

        private void AddEntry(StackPanel list, string version, string[] changes)
        {
            list.Children.Add(new TextBlock
            {
                Text = version,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 6),
                Foreground = Brushes.White
            });

            foreach (var change in changes)
            {
                var p = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 2, 0, 0) };
                p.Children.Add(new TextBlock { Text = "•", Foreground = Brushes.LightGray, Margin = new Thickness(0, 0, 6, 0) });
                p.Children.Add(new TextBlock { Text = change, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap, Width = 600 });
                list.Children.Add(p);
            }
        }
    }
}