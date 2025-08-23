using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class ChangelogPage : Page
    {
        public ChangelogPage()
        {
            var scrollViewer = new ScrollViewer();
            var panel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                Margin = new Thickness(20) // Fixed - using single parameter constructor
            };
            
            panel.Children.Add(new TextBlock
            {
                Text = "What's New",
                FontSize = 24,
                Margin = new Thickness(0, 0, 0, 20), // Fixed - using all four parameters
                Foreground = Brushes.White
            });
            
            // Add change log entries
            AddChangelogEntry(panel, "v1.0.0", "2023-07-15", new[]
            {
                "Initial release of CM Launcher",
                "Added support for CMZ and CMW",
                "Basic settings and configuration"
            });
            
            AddChangelogEntry(panel, "v0.9.5-beta", "2023-07-01", new[]
            {
                "Beta testing version",
                "Fixed multiple bugs in the launcher interface",
                "Improved mod loading performance"
            });
            
            scrollViewer.Content = panel;
            Content = scrollViewer;
        }
        
        private void AddChangelogEntry(StackPanel panel, string version, string date, string[] changes)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"{version} ({date})",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 5), // Fixed - using all four parameters
                Foreground = Brushes.White
            });
            
            var changesList = new StackPanel
            {
                Margin = new Thickness(10, 0, 0, 15) // Fixed - using all four parameters
            };
            
            foreach (var change in changes)
            {
                changesList.Children.Add(new TextBlock
                {
                    Text = $"• {change}",
                    FontSize = 14,
                    Margin = new Thickness(0, 3, 0, 0), // Fixed - using all four parameters
                    Foreground = Brushes.LightGray,
                    TextWrapping = TextWrapping.Wrap
                });
            }
            
            panel.Children.Add(changesList);
        }
    }
}