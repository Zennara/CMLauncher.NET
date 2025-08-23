using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class ModContentPage : Page
    {
        public ModContentPage(string modType)
        {
            // Ensure the page itself is transparent so the window backdrop shows through
            this.Background = Brushes.Transparent;

            // Create main grid layout (transparent)
            var grid = new Grid
            {
                Background = Brushes.Transparent
            };

            // Remove internal banner/background so global BackdropImage is visible
            // Add news scroller at the bottom
            var newsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 120,
                Margin = new Thickness(20)
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
                Margin = new Thickness(5, 0, 5, 0),
                CornerRadius = new CornerRadius(3)
            };
            
            var panel = new Grid { Background = Brushes.Transparent };
            
            // Placeholder transparent image layer (kept for possible future content)
            var image = new System.Windows.Controls.Image
            {
                Stretch = Stretch.UniformToFill,
                Opacity = 0.7
            };
            panel.Children.Add(image);
            
            var overlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(10, 5, 10, 5)
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