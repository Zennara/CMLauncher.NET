using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CMLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string SteamIconName = "Lantern.png"; // expected filename in assets/blocks

        private ComboBox InstanceComboBox;
        private string currentSidebarSelection = "CMZ"; // Track current sidebar selection
        private Button currentlySelectedSidebarButton; // Keep track of the currently selected sidebar button
        private Button _selectedInstallButton;

        private readonly SolidColorBrush DefaultForegroundBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        public MainWindow()
        {
            InitializeComponent();

            // Ensure required directories exist
            InstallationService.EnsureDirectoryStructure();
            
            InstanceComboBox = FindName("InstanceComboBox") as ComboBox;
            
            NavigateToContent("CMZ");
            LoadInstallationsIntoPopup("CMZ");
            
            Loaded += (s, e) => 
            {
                try
                {
                    SelectSidebarButton(btnCMZ);
                    Debug.WriteLine("Successfully selected CMZ button on startup");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR during initial button selection: {ex.Message}");
                }
            };
        }

        private void LoadInstallationsIntoPopup(string gameKey)
        {
            InstallItemsPanel.Children.Clear();

            // Default Steam entry uses the Lantern icon
            InstallItemsPanel.Children.Add(CreateInstallMenuButton("Steam", "Latest Version", SteamIconName));

            var installs = InstallationService.LoadInstallations(gameKey);
            foreach (var inst in installs)
            {
                InstallItemsPanel.Children.Add(CreateInstallMenuButton(inst.Name, inst.Version, inst.IconName));
            }

            // Select the first item by default (Steam)
            var firstButton = InstallItemsPanel.Children.OfType<Button>().FirstOrDefault();
            if (firstButton != null)
            {
                if (_selectedInstallButton != null)
                {
                    SelectionProperties.SetIsSelected(_selectedInstallButton, false);
                }
                _selectedInstallButton = firstButton;
                SelectionProperties.SetIsSelected(_selectedInstallButton, true);

                if (firstButton.Tag is string tag)
                {
                    var parts = tag.Split('|');
                    if (parts.Length >= 2)
                    {
                        SelectedInstallName.Text = parts[0];
                        SelectedInstallVersion.Text = parts[1];
                        var iconName = parts.Length >= 3 ? parts[2] : SteamIconName;
                        UpdateSelectedIcon(iconName);
                    }
                }
            }
        }

        private Button CreateInstallMenuButton(string name, string version, string? iconName)
        {
            var button = new Button
            {
                Style = (Style)FindResource("InstallMenuItemStyle"),
                Tag = $"{name}|{version}|{iconName}",
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var img = new Image { Width = 24, Height = 24, Stretch = Stretch.Uniform, Margin = new Thickness(0) };
            if (!string.IsNullOrWhiteSpace(iconName))
            {
                TryLoadBlockIcon(img, iconName);
            }
            Grid.SetColumn(img, 0);
            grid.Children.Add(img);

            var textStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            Grid.SetColumn(textStack, 1);
            textStack.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            textStack.Children.Add(new TextBlock { Text = version, Foreground = new SolidColorBrush(Color.FromRgb(207, 207, 207)), FontSize = 12 });
            grid.Children.Add(textStack);

            button.Content = grid;
            button.Click += SelectInstallation_Click;
            return button;
        }

        private bool TryLoadBlockIcon(Image target, string iconName)
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "blocks", iconName);
                if (!File.Exists(path)) return false;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                target.Source = bmp;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Brush GetAccentBrush()
        {
            return (Brush)FindResource("AccentBrush");
        }

        private void UpdateSelectedIcon(string? iconName)
        {
            // SelectedInstallIcon may not be connected yet during InitializeComponent
            var img = this.FindName("SelectedInstallIcon") as System.Windows.Controls.Image;
            if (img == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(iconName))
            {
                img.Source = null;
                return;
            }
            TryLoadBlockIcon(img, iconName);
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                foreach (var child in TabsPanel.Children)
                {
                    if (child is Button b) b.BorderThickness = new Thickness(0);
                }

                button.BorderThickness = new Thickness(0, 0, 0, 2);
                button.BorderBrush = GetAccentBrush();

                switch (tag)
                {
                    case "Play":
                        NavigateToContent(currentSidebarSelection);
                        break;
                    case "Installations":
                        MainContentFrame.Navigate(new InstallationsPage(currentSidebarSelection));
                        break;
                    case "PatchNotes":
                        break;
                }
            }
        }

        private void SidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.Tag is string tag)
            {
                if (clickedButton == currentlySelectedSidebarButton)
                    return;
                
                SelectSidebarButton(clickedButton);

                if (tag == "CMZ" || tag == "CMW")
                {
                    currentSidebarSelection = tag;
                    EditionTitleText.Text = tag == "CMZ" ? "CASTLEMINER Z" : "CM WARFARE";
                    LoadInstallationsIntoPopup(tag);
                }
                
                NavigateToContent(tag);
            }
        }
        
        private void SelectSidebarButton(Button buttonToSelect)
        {
            // Clear previous selection across both panels: remove attached property and reset fg
            foreach (var child in SidebarPanel.Children)
            {
                if (child is Button b)
                {
                    SelectionProperties.SetIsSelected(b, false);
                    b.ClearValue(Button.BackgroundProperty);
                    b.SetCurrentValue(Button.ForegroundProperty, DefaultForegroundBrush);
                }
            }
            foreach (var child in BottomSidebarPanel.Children)
            {
                if (child is Button b)
                {
                    SelectionProperties.SetIsSelected(b, false);
                    b.ClearValue(Button.BackgroundProperty);
                    b.SetCurrentValue(Button.ForegroundProperty, DefaultForegroundBrush);
                }
            }

            // Mark current as selected using attached property (style will pick it up)
            SelectionProperties.SetIsSelected(buttonToSelect, true);
            buttonToSelect.SetCurrentValue(Button.ForegroundProperty, Brushes.White);

            currentlySelectedSidebarButton = buttonToSelect;
        }

        private void SetBackdropFor(string tag)
        {
            // Backdrop stays the same
            var fileName = tag == "CMW" ? "cmw-backdrop.png" : "cmz-backdrop.png";
            var relPath = $"assets/backdrops/{fileName}";
            LoadImageWithFallback(BackdropImage, relPath, 0.9);

            // Wordart: try multiple candidate filenames
            string[] candidates = tag == "CMW"
                ? new[]
                {
                    "assets/wordarts/castleminerwarfare.png",
                    "assets/wordarts/castleminerwarfare.jpg",
                    "assets/wordarts/castleminerwarfare"
                }
                : new[]
                {
                    "assets/wordarts/castleminerz.png",
                    "assets/wordarts/castleminerz.jpg",
                    "assets/wordarts/castleminerz"
                };
            LoadFirstAvailable(WordartImage, candidates, 0.95);
        }

        private void LoadFirstAvailable(System.Windows.Controls.Image target, string[] relCandidates, double opacity)
        {
            foreach (var rel in relCandidates)
            {
                if (LoadImageWithFallback(target, rel, opacity)) return;
            }
            target.Source = null;
            target.Visibility = Visibility.Collapsed;
        }

        private bool LoadImageWithFallback(System.Windows.Controls.Image target, string relPath, double opacity)
        {
            bool SetFromUri(string uriString)
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(uriString, UriKind.RelativeOrAbsolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    target.Source = bmp;
                    target.Visibility = Visibility.Visible;
                    target.Opacity = opacity;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Image try failed for '{uriString}': {ex.Message}");
                    return false;
                }
            }

            // 1) Application resource
            if (SetFromUri($"pack://application:,,,/{relPath}")) return true;
            // 2) Site of origin
            if (SetFromUri($"pack://siteoforigin:,,,/{relPath}")) return true;
            // 3) Local disk
            var diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(diskPath) && SetFromUri(diskPath)) return true;
            return false;
        }

        private void NavigateToContent(string contentTag)
        {
            switch (contentTag)
            {
                case "Home":
                    BackdropImage.Visibility = Visibility.Collapsed;
                    WordartImage.Visibility = Visibility.Collapsed;
                    MainContentFrame.Navigate(new HomePage());
                    break;
                case "CMZ":
                    SetBackdropFor("CMZ");
                    MainContentFrame.Navigate(new ModContentPage("CMZ"));
                    break;
                case "CMW":
                    SetBackdropFor("CMW");
                    MainContentFrame.Navigate(new ModContentPage("CMW"));
                    break;
                case "Settings":
                    BackdropImage.Visibility = Visibility.Collapsed;
                    WordartImage.Visibility = Visibility.Collapsed;
                    MainContentFrame.Navigate(new SettingsPage());
                    break;
                case "Changelog":
                    BackdropImage.Visibility = Visibility.Collapsed;
                    WordartImage.Visibility = Visibility.Collapsed;
                    MainContentFrame.Navigate(new ChangelogPage());
                    break;
            }

            if (contentTag == "CMZ" || contentTag == "CMW")
            {
                SelectTabButton("Play");
            }
        }
        
        private void SelectTabButton(string tag)
        {
            var accent = GetAccentBrush();
            foreach (var child in TabsPanel.Children)
            {
                if (child is Button b)
                {
                    if (b.Tag?.ToString() == tag)
                    {
                        b.BorderThickness = new Thickness(0, 0, 0, 2);
                        b.BorderBrush = accent;
                    }
                    else
                    {
                        b.BorderThickness = new Thickness(0);
                    }
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = SelectedInstallName.Text?.Trim();
                var version = SelectedInstallVersion.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("No installation selected."); return; }

                if (string.Equals(name, "Steam", StringComparison.OrdinalIgnoreCase))
                {
                    var exeName = currentSidebarSelection == InstallationService.CMWKey ? "CastleMinerWarfare.exe" : "CastleMinerZ.exe";

                    // Prefer actual Steam install directory
                    var steamDir = SteamLocator.FindGamePath(InstallationService.GetAppId(currentSidebarSelection));
                    string? gameDir = steamDir;

                    // If user picked a specific version and steamDir not found, try versions folder
                    if (string.IsNullOrWhiteSpace(gameDir))
                    {
                        var versionsRoot = InstallationService.GetVersionsPath(currentSidebarSelection);
                        if (!string.IsNullOrWhiteSpace(version) &&
                            !string.Equals(version, "Latest Version", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(version, "Steam Version", StringComparison.OrdinalIgnoreCase))
                        {
                            var candidate = Path.Combine(versionsRoot, version);
                            if (Directory.Exists(candidate)) gameDir = candidate;
                        }
                        if (gameDir == null) gameDir = versionsRoot;
                    }

                    InstallationService.EnsureSteamAppId(currentSidebarSelection, gameDir!);
                    var exePath = Path.Combine(gameDir!, exeName);
                    if (File.Exists(exePath))
                    {
                        Process.Start(new ProcessStartInfo { FileName = exePath, WorkingDirectory = gameDir, UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("Executable not found for Steam entry. Install a version first.", "CM Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                // Custom installations
                var installs = InstallationService.LoadInstallations(currentSidebarSelection);
                var info = installs.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
                if (info == null)
                {
                    MessageBox.Show("Selected installation not found.");
                    return;
                }

                var exe = currentSidebarSelection == InstallationService.CMWKey ? "CastleMinerWarfare.exe" : "CastleMinerZ.exe";
                var game = Path.Combine(info.RootPath, "Game");
                var exePath2 = Path.Combine(game, exe);
                InstallationService.EnsureSteamAppId(currentSidebarSelection, game);
                if (File.Exists(exePath2))
                {
                    Process.Start(new ProcessStartInfo { FileName = exePath2, WorkingDirectory = game, UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"Executable not found: {exePath2}", "CM Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch: {ex.Message}", "CM Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InstanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string instanceName = selectedItem.Content?.ToString() ?? "Unknown";
            }
        }

        private void SelectInstallation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                // Clear previous selection visual
                if (_selectedInstallButton != null)
                {
                    SelectionProperties.SetIsSelected(_selectedInstallButton, false);
                }

                // Mark new selection
                _selectedInstallButton = btn;
                SelectionProperties.SetIsSelected(_selectedInstallButton, true);

                // Update toggle texts from Tag "name|version|icon"
                var parts = tag.Split('|');
                if (parts.Length >= 2)
                {
                    SelectedInstallName.Text = parts[0];
                    SelectedInstallVersion.Text = parts[1];
                    var iconName = parts.Length >= 3 ? parts[2] : null;
                    UpdateSelectedIcon(iconName);
                }

                // Close popup
                InstallToggle.IsChecked = false;
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // Close the popup
            if (AccountToggle != null)
            {
                AccountToggle.IsChecked = false;
            }
            
            // TODO: Insert real sign-out logic here
            MessageBox.Show("Logged out.", "Account", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void RefreshInstallationsMenu()
        {
            // Refresh using the currently selected game (CMZ/CMW)
            LoadInstallationsIntoPopup(currentSidebarSelection);
        }
    }
}