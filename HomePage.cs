using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class HomePage : Page
    {
        public HomePage()
        {
            var panel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                Margin = new Thickness(20) // Fixed - using single parameter constructor
            };
            
            panel.Children.Add(new TextBlock
            {
                Text = "Welcome to CM Launcher",
                FontSize = 24,
                Margin = new Thickness(0, 0, 0, 20), // Fixed - using all four parameters
                Foreground = Brushes.White
            });
            
            panel.Children.Add(new TextBlock
            {
                Text = "Select a game from the sidebar to play",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 20), // Fixed - using all four parameters
                Foreground = Brushes.LightGray
            });
            
            Content = panel;
        }
    }
}