using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CMLauncher
{
    public class ModContentPage : Page
    {
        public ModContentPage(string modType)
        {
            // Create main grid layout
            var grid = new Grid();
            
            // Create a placeholder for game artwork/banner
            var banner = new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Set different banners based on mod type
            try
            {
                if (modType == "CMZ")
                {
                    banner.Source = new BitmapImage(new System.Uri("pack://application:,,,/Images/cmz_banner.jpg", System.UriKind.Absolute));
                }
                else if (modType == "CMW")
                {
                    banner.Source = new BitmapImage(new System.Uri("pack://application:,,,/Images/cmw_banner.jpg", System.UriKind.Absolute));
                }
            }
            catch
            {
                // If images aren't found, create a colored background instead
                grid.Background = modType == "CMZ" ? 
                    new LinearGradientBrush(
                        Color.FromRgb(150, 90, 80), 
                        Color.FromRgb(80, 40, 30), 
                        new Point(0, 0), 
                        new Point(1, 1)) :
                    new LinearGradientBrush(
                        Color.FromRgb(70, 90, 120), 
                        Color.FromRgb(30, 40, 70), 
                        new Point(0, 0), 
                        new Point(1, 1));
            }
            
            grid.Children.Add(banner);
            
            // Add news scroller at the bottom
            var newsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 120,
                Margin = new Thickness(20) // Fixed - using single parameter constructor
            };
            
            // Add some news items
            for (int i = 0; i < 5; i++)
            {
                var newsItem = CreateNewsItem($"News {i + 1}", modType);
                newsPanel.Children.Add(newsItem);
            }
            
            grid.Children.Add(newsPanel);
            
            Content = grid;
        }
        
        private Border CreateNewsItem(string title, string modType)
        {
            var border = new Border
            {
                Width = 180,
                Height = 100,
                Background = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
                Margin = new Thickness(5, 0, 5, 0), // Fixed - using all four parameters
                CornerRadius = new CornerRadius(3)
            };
            
            var panel = new Grid();
            
            var image = new Image
            {
                Stretch = Stretch.UniformToFill,
                Opacity = 0.7
            };
            
            // Set placeholder image
            panel.Children.Add(image);
            
            var overlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(10, 5, 10, 5) // Fixed - using all four parameters
            };
            
            var titleText = new TextBlock
            {
                Text = $"{modType} {title}",
                Foreground = Brushes.White,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };
            
            overlay.Child = titleText;
            panel.Children.Add(overlay);
            
            border.Child = panel;
            
            return border;
        }
    }
}