using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class InstallationsPage : Page
    {
        public InstallationsPage()
        {
            var grid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32))
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20) // Fixed - using single parameter constructor
            };
            
            // Title
            panel.Children.Add(new TextBlock
            {
                Text = "Installations",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20), // Fixed - using all four parameters
                Foreground = Brushes.White
            });
            
            // Description
            panel.Children.Add(new TextBlock
            {
                Text = "Create and manage your installations of CMZ and CMW",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20), // Fixed - using all four parameters
                Foreground = Brushes.LightGray
            });
            
            // Create button
            var createButton = new Button
            {
                Content = "New Installation",
                Padding = new Thickness(15, 8, 15, 8), // Fixed - using all four parameters
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 20) // Fixed - using all four parameters
            };
            panel.Children.Add(createButton);
            
            // Installations list
            var installationsPanel = new StackPanel();
            
            AddInstallationItem(installationsPanel, "CMZ Default", "1.21.5-forge-55.0.23", true);
            AddInstallationItem(installationsPanel, "CMW Default", "1.20.1-forge-52.1.16", false);
            AddInstallationItem(installationsPanel, "CMZ Testing", "1.21.5-forge-55.0.24-beta", false);
            
            panel.Children.Add(installationsPanel);
            
            grid.Children.Add(panel);
            Content = grid;
        }
        
        private void AddInstallationItem(StackPanel panel, string name, string version, bool isSelected)
        {
            var border = new Border
            {
                Background = isSelected ? 
                    new SolidColorBrush(Color.FromRgb(60, 60, 60)) : 
                    new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Margin = new Thickness(0, 0, 0, 10), // Fixed - using all four parameters
                Padding = new Thickness(15), // Fixed - using single parameter constructor
                CornerRadius = new CornerRadius(3)
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var infoPanel = new StackPanel();
            
            infoPanel.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 16,
                Foreground = Brushes.White
            });
            
            infoPanel.Children.Add(new TextBlock
            {
                Text = $"Version: {version}",
                FontSize = 12,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 5, 0, 0) // Fixed - using all four parameters
            });
            
            Grid.SetColumn(infoPanel, 0);
            grid.Children.Add(infoPanel);
            
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var playButton = new Button
            {
                Content = "Play",
                Padding = new Thickness(15, 5, 15, 5), // Fixed - using all four parameters
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 10, 0) // Fixed - using all four parameters
            };
            
            var editButton = new Button
            {
                Content = "...",
                Padding = new Thickness(10, 5, 10, 5), // Fixed - using all four parameters
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            
            buttonsPanel.Children.Add(playButton);
            buttonsPanel.Children.Add(editButton);
            
            Grid.SetColumn(buttonsPanel, 1);
            grid.Children.Add(buttonsPanel);
            
            border.Child = grid;
            panel.Children.Add(border);
        }
    }
}