using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class InstallationsPage : Page
    {
        private StackPanel _listHost = null!;
        private readonly string _gameKey;

        public InstallationsPage(string gameKey)
        {
            _gameKey = gameKey;
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));

            var root = new StackPanel { Margin = new Thickness(20) };

            // Header
            root.Children.Add(new TextBlock
            {
                Text = "Installations",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            });

            _listHost = new StackPanel();
            root.Children.Add(_listHost);

            Content = root;

            RefreshList();
        }

        private void RefreshList()
        {
            _listHost.Children.Clear();

            // Always include default Steam option first
            AddInstallationItem(_listHost, "Steam", "Latest Version", true);

            var installs = InstallationService.LoadInstallations(_gameKey);
            foreach (var inst in installs)
            {
                AddInstallationItem(_listHost, inst.Name, inst.Version, false);
            }
        }
        
        private void AddInstallationItem(StackPanel panel, string name, string version, bool isSelected)
        {
            var border = new Border
            {
                Background = isSelected ? 
                    new SolidColorBrush(Color.FromRgb(60, 60, 60)) : 
                    new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(15),
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
                Margin = new Thickness(0, 5, 0, 0)
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
                Padding = new Thickness(15, 5, 15, 5),
                Background = (Brush)Application.Current.MainWindow.FindResource("AccentBrush"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 10, 0)
            };
            
            var editButton = new Button
            {
                Content = "...",
                Padding = new Thickness(10, 5, 10, 5),
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