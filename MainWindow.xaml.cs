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
        private string currentSidebarSelection = "CMZ"; // Track current sidebar selection

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
                        // Navigate back to the currently selected mod content
                        NavigateToContent(currentSidebarSelection);
                        break;
                    case "Installations":
                        MainContentFrame.Navigate(new InstallationsPage());
                        break;
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
                
                // Store current selection if it's a mod (CMZ or CMW)
                if (tag == "CMZ" || tag == "CMW")
                {
                    currentSidebarSelection = tag;
                }
                
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
            
            // Find and select the Play tab when showing content pages
            if (contentTag == "CMZ" || contentTag == "CMW")
            {
                SelectTabButton("Play");
            }
        }
        
        private void SelectTabButton(string tag)
        {
            // Find and visually select the tab button with the given tag
            foreach (var child in FindVisualChildren<Button>(this))
            {
                if (child.Tag?.ToString() == tag && child.Style == FindResource("TabButtonStyle"))
                {
                    // Reset all tab buttons
                    if (child.Parent is StackPanel tabPanel)
                    {
                        foreach (var tabChild in tabPanel.Children)
                        {
                            if (tabChild is Button b)
                            {
                                b.BorderThickness = new Thickness(0);
                            }
                        }
                    }
                    
                    // Select this tab button
                    child.BorderThickness = new Thickness(0, 0, 0, 2);
                    child.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    break;
                }
            }
        }
        
        // Helper method to find visual children of a specified type
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
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