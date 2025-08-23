using System.Linq;
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

            // Root layout uses Grid so the ScrollViewer gets a constrained height and can scroll
            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header with button
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.Children.Add(new TextBlock
            {
                Text = "Installations",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            });
            var newBtn = new Button
            {
                Content = "New Installation",
                Padding = new Thickness(15, 8, 15, 8),
                Background = (Brush)Application.Current.MainWindow.FindResource("AccentBrush"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(10, 0, 0, 20)
            };
            newBtn.Click += (s, e) => ShowCreateDialog();
            Grid.SetColumn(newBtn, 1);
            headerGrid.Children.Add(newBtn);
            Grid.SetRow(headerGrid, 0);
            root.Children.Add(headerGrid);

            // Scrollable list area
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26))
            };
            _listHost = new StackPanel();
            scroll.Content = _listHost;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            Content = root;

            RefreshList();
        }

        private void ShowCreateDialog()
        {
            var dlg = new Window
            {
                Title = "Create new installation",
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = new SolidColorBrush(Color.FromRgb(27, 27, 27)),
                Foreground = Brushes.White,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock { Text = "Name", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            var nameBox = new TextBox { Width = 360, Text = "Unnamed installation" };
            panel.Children.Add(nameBox);

            panel.Children.Add(new TextBlock { Text = "Version", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 6) });
            var versionCombo = new ComboBox { Width = 360 };
            versionCombo.Items.Add("Steam Version");
            foreach (var v in InstallationService.LoadAvailableVersions(_gameKey))
            {
                versionCombo.Items.Add(v);
            }
            versionCombo.SelectedIndex = 0;
            panel.Children.Add(versionCombo);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(14, 6, 14, 6) };
            cancel.Click += (s, e) => dlg.Close();
            var create = new Button { Content = "Install", Padding = new Thickness(14, 6, 14, 6), Background = (Brush)Application.Current.MainWindow.FindResource("AccentBrush"), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            create.Click += (s, e) =>
            {
                var name = string.IsNullOrWhiteSpace(nameBox.Text) ? "Unnamed installation" : nameBox.Text.Trim();
                var version = versionCombo.SelectedItem?.ToString() ?? "Steam Version";
                InstallationService.CreateInstallation(_gameKey, name, version);
                dlg.Close();
                RefreshList();
            };
            buttons.Children.Add(cancel);
            buttons.Children.Add(create);
            panel.Children.Add(buttons);

            dlg.Content = panel;
            dlg.ShowDialog();
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