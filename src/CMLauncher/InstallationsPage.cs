using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CMLauncher
{
    public class InstallationsPage : Page
    {
        private const string SteamIconName = "Lantern.png"; // icon file in assets/blocks

        private StackPanel _listHost = null!;
        private readonly string _gameKey;

        public InstallationsPage(string gameKey)
        {
            _gameKey = gameKey;
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));

            var root = new Grid { Margin = new Thickness(20) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

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

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.Transparent
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

            // Icon selector - centered with small caret toggle
            var iconArea = new Grid { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };
            iconArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            iconArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var currentIcon = new Image { Width = 56, Height = 56, Stretch = Stretch.Uniform };
            var allIcons = InstallationService.LoadAvailableIcons();
            string? selectedIcon = null;
            // pick a random default for preview and selection
            if (allIcons.Count > 0)
            {
                var rnd = new System.Random();
                selectedIcon = allIcons[rnd.Next(allIcons.Count)];
            }
            SetIconImage(currentIcon, selectedIcon);
            Grid.SetColumn(currentIcon, 0);
            iconArea.Children.Add(currentIcon);

            var caretToggle = new ToggleButton
            {
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(6, 0, 6, 0),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                FocusVisualStyle = null,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Content = "\uE70D"
            };
            Grid.SetColumn(caretToggle, 1);
            iconArea.Children.Add(caretToggle);

            var iconPopup = new Popup
            {
                PlacementTarget = caretToggle,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };
            var iconScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 260, Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)) };
            var iconWrap = new WrapPanel { Margin = new Thickness(8) };
            foreach (var iconName in allIcons)
            {
                var img = new Image { Width = 40, Height = 40, Stretch = Stretch.Uniform, Margin = new Thickness(4) };
                SetIconImage(img, iconName);
                var btn = new Button { Padding = new Thickness(0), BorderThickness = new Thickness(0), Background = Brushes.Transparent, Tag = iconName, Content = img };
                btn.Click += (s, e) => { selectedIcon = iconName; SetIconImage(currentIcon, selectedIcon); iconPopup.IsOpen = false; caretToggle.IsChecked = false; };
                iconWrap.Children.Add(btn);
            }
            iconScroll.Content = iconWrap;
            iconPopup.Child = new Border { Width = 420, Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 48)), Child = iconScroll };
            caretToggle.Checked += (s, e) => iconPopup.IsOpen = true;
            caretToggle.Unchecked += (s, e) => iconPopup.IsOpen = false;
            iconPopup.Closed += (s, e) => { caretToggle.IsChecked = false; };

            panel.Children.Add(iconArea);

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
                InstallationService.CreateInstallation(_gameKey, name, version, selectedIcon);
                dlg.Close();
                RefreshList();
                // Also refresh the bottom-left selector immediately
                if (Application.Current?.MainWindow is MainWindow mw)
                {
                    mw.RefreshInstallationsMenu();
                }
            };
            buttons.Children.Add(cancel);
            buttons.Children.Add(create);
            panel.Children.Add(buttons);

            dlg.Content = panel;
            dlg.ShowDialog();
        }

        private static void SetIconImage(Image img, string? iconName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(iconName)) { img.Source = null; return; }
                var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "assets", "blocks", iconName);
                if (!System.IO.File.Exists(path)) { img.Source = null; return; }
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new System.Uri(path, System.UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                img.Source = bmp;
            }
            catch
            {
                img.Source = null;
            }
        }

        private void RefreshList()
        {
            _listHost.Children.Clear();

            // Steam pseudo-installation first, with Lantern icon
            AddInstallationItem(_listHost, new InstallationInfo { GameKey = _gameKey, Name = "Steam", Version = "Latest Version", IconName = SteamIconName, RootPath = "" }, true);

            var installs = InstallationService.LoadInstallations(_gameKey);
            foreach (var inst in installs)
            {
                AddInstallationItem(_listHost, inst, false);
            }
        }
        
        private void AddInstallationItem(StackPanel panel, InstallationInfo info, bool isSelected)
        {
            var border = new Border
            {
                Background = isSelected ? new SolidColorBrush(Color.FromRgb(60, 60, 60)) : new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(3)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            var iconHost = new Border { Width = 48, Height = 48, Background = Brushes.Transparent, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            var img = new Image { Stretch = Stretch.Uniform };
            if (!string.IsNullOrWhiteSpace(info.IconName))
            {
                try
                {
                    var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "assets", "blocks", info.IconName);
                    if (System.IO.File.Exists(path))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new System.Uri(path, System.UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        img.Source = bmp;
                    }
                }
                catch { }
            }
            iconHost.Child = img;
            Grid.SetColumn(iconHost, 0);
            grid.Children.Add(iconHost);

            // Info
            var infoPanel = new StackPanel();
            infoPanel.Children.Add(new TextBlock { Text = info.Name, FontSize = 16, Foreground = Brushes.White });
            infoPanel.Children.Add(new TextBlock { Text = $"Version: {info.Version}", FontSize = 12, Foreground = Brushes.LightGray, Margin = new Thickness(0, 5, 0, 0) });
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            // Actions
            var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };

            var playBtn = new Button
            {
                Content = "Play",
                Padding = new Thickness(15, 6, 15, 6),
                Background = (Brush)Application.Current.MainWindow.FindResource("AccentBrush"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0)
            };
            playBtn.Click += (s, e) => MessageBox.Show($"Launching {info.Name}...");
            actions.Children.Add(playBtn);

            // Folder icon (Segoe MDL2 Assets: E8B7)
            var folderBtn = new Button
            {
                Padding = new Thickness(10, 6, 10, 6),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0),
                ToolTip = "Open folder"
            };
            folderBtn.Content = new TextBlock { Text = "\uE8B7", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, Foreground = Brushes.White };
            folderBtn.Click += (s, e) =>
            {
                try
                {
                    var path = string.IsNullOrEmpty(info.RootPath) ? InstallationService.GetInstallationsPath(info.GameKey) : info.RootPath;
                    if (System.IO.Directory.Exists(path))
                        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                }
                catch { }
            };
            actions.Children.Add(folderBtn);

            // More (three dots) icon (Segoe MDL2 Assets: E712)
            var menuToggle = new ToggleButton
            {
                Padding = new Thickness(10, 6, 10, 6),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ToolTip = "More"
            };
            menuToggle.Content = new TextBlock { Text = "\uE712", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, Foreground = Brushes.White };
            var menuPopup = new Popup { PlacementTarget = menuToggle, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true };
            var menuStack = new StackPanel();
            menuStack.Children.Add(CreateMenuItem("Edit", () => { menuPopup.IsOpen = false; menuToggle.IsChecked = false; }));
            menuStack.Children.Add(CreateMenuItem("Duplicate", () => { menuPopup.IsOpen = false; menuToggle.IsChecked = false; var dup = InstallationService.DuplicateInstallation(info); RefreshList(); if (Application.Current?.MainWindow is MainWindow mw) mw.RefreshInstallationsMenu(); }));
            menuStack.Children.Add(CreateMenuItem("Delete", () => { menuPopup.IsOpen = false; menuToggle.IsChecked = false; InstallationService.DeleteInstallation(info); RefreshList(); if (Application.Current?.MainWindow is MainWindow mw) mw.RefreshInstallationsMenu(); }));
            menuPopup.Child = new Border { Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)), BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)), BorderThickness = new Thickness(1), Child = menuStack };
            menuToggle.Checked += (s, e) => menuPopup.IsOpen = true;
            menuToggle.Unchecked += (s, e) => menuPopup.IsOpen = false;
            menuPopup.Closed += (s, e) => menuToggle.IsChecked = false;
            actions.Children.Add(menuToggle);

            Grid.SetColumn(actions, 2);
            grid.Children.Add(actions);

            border.MouseEnter += (s, e) => actions.Visibility = Visibility.Visible;
            border.MouseLeave += (s, e) => { if (menuPopup.IsOpen == false) actions.Visibility = Visibility.Collapsed; };

            border.Child = grid;
            panel.Children.Add(border);
        }

        private UIElement CreateMenuItem(string text, System.Action onClick)
        {
            var btn = new Button { Content = text, Padding = new Thickness(12, 8, 12, 8), Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), HorizontalContentAlignment = HorizontalAlignment.Left };
            btn.Click += (s, e) => onClick();
            btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(Color.FromRgb(70, 70, 70));
            btn.MouseLeave += (s, e) => btn.Background = Brushes.Transparent;
            return btn;
        }
    }
}