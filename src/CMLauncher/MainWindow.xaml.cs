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

            // Always include default Steam option first
            InstallItemsPanel.Children.Add(CreateInstallMenuButton("Steam", "Latest Version"));

            var installs = InstallationService.LoadInstallations(gameKey);
            foreach (var inst in installs)
            {
                InstallItemsPanel.Children.Add(CreateInstallMenuButton(inst.Name, inst.Version));
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
                    if (parts.Length == 2)
                    {
                        SelectedInstallName.Text = parts[0];
                        SelectedInstallVersion.Text = parts[1];
                    }
                }
            }
        }

        private Button CreateInstallMenuButton(string name, string version)
        {
            var button = new Button
            {
                Style = (Style)FindResource("InstallMenuItemStyle"),
                Tag = $"{name}|{version}",
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var colorBlock = new Border
            {
                Width = 28,
                Height = 20,
                CornerRadius = new CornerRadius(3),
                Background = (Brush)FindResource("AccentBrush")
            };
            grid.Children.Add(colorBlock);

            var textStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            Grid.SetColumn(textStack, 1);
            textStack.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            textStack.Children.Add(new TextBlock { Text = version, Foreground = new SolidColorBrush(Color.FromRgb(207, 207, 207)), FontSize = 12 });
            grid.Children.Add(textStack);

            button.Content = grid;
            button.Click += SelectInstallation_Click;
            return button;
        }

        private Brush GetAccentBrush()
        {
            // Safely fetch the accent brush defined in XAML resources
            return (Brush)FindResource("AccentBrush");
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
            MessageBox.Show("Launching game...", "CM Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
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

                // Update toggle texts from Tag "name|version"
                var parts = tag.Split('|');
                if (parts.Length == 2)
                {
                    SelectedInstallName.Text = parts[0];
                    SelectedInstallVersion.Text = parts[1];
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
    }
}