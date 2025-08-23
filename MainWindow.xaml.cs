using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Define the InstanceComboBox if it's not in XAML
        private ComboBox InstanceComboBox;

        public MainWindow()
        {
            InitializeComponent();
            
            // Find the InstanceComboBox in the visual tree after initialization
            InstanceComboBox = FindName("InstanceComboBox") as ComboBox;
            
            // Set default content
            NavigateToContent("CMZ");
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                // Update selected tab styling - Fixed UIElementCollection access
                if (button.Parent is StackPanel tabPanel)
                {
                    foreach (var child in tabPanel.Children)
                    {
                        if (child is Button b)
                        {
                            b.BorderThickness = new Thickness(0);
                        }
                    }
                }
                
                button.BorderThickness = new Thickness(0, 0, 0, 2);
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                
                // Handle tab navigation
                switch (tag)
                {
                    case "Play":
                        // Already on play screen
                        break;
                    case "Installations":
                        MainContentFrame.Navigate(new InstallationsPage());
                        break;
                    case "Skins":
                    case "PatchNotes":
                        // Would navigate to patch notes page
                        break;
                }
            }
        }

        private void SidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                // Update selected sidebar button styling - Fixed UIElementCollection access
                if (button.Parent is StackPanel sidebarPanel)
                {
                    foreach (var child in sidebarPanel.Children)
                    {
                        if (child is Button b)
                        {
                            b.Background = new SolidColorBrush(Colors.Transparent);
                            b.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                        }
                    }
                }
                
                button.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                button.Foreground = new SolidColorBrush(Colors.White);
                
                NavigateToContent(tag);
            }
        }

        private void NavigateToContent(string contentTag)
        {
            // Navigate to the specific content page
            switch (contentTag)
            {
                case "Home":
                    MainContentFrame.Navigate(new HomePage());
                    break;
                case "CMZ":
                    MainContentFrame.Navigate(new ModContentPage("CMZ"));
                    break;
                case "CMW":
                    MainContentFrame.Navigate(new ModContentPage("CMW"));
                    break;
                case "Settings":
                    MainContentFrame.Navigate(new SettingsPage());
                    break;
                case "Changelog":
                    MainContentFrame.Navigate(new ChangelogPage());
                    break;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle launching the game/mod
            MessageBox.Show("Launching game...", "CM Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InstanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle instance selection
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string instanceName = selectedItem.Content?.ToString() ?? "Unknown";
                // Update based on selection
            }
        }
    }
}