using System;
using System.Diagnostics;
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
        private Button currentlySelectedSidebarButton; // Keep track of the currently selected sidebar button

        public MainWindow()
        {
            InitializeComponent();
            
            // Find the InstanceComboBox in the visual tree after initialization
            InstanceComboBox = FindName("InstanceComboBox") as ComboBox;
            
            // Set default content
            NavigateToContent("CMZ");
            
            // Select CMZ button by default when UI is loaded
            Loaded += (s, e) => 
            {
                try
                {
                    // Use direct reference to the named CMZ button
                    btnCMZ.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                    btnCMZ.Foreground = Brushes.White;
                    currentlySelectedSidebarButton = btnCMZ;
                    Debug.WriteLine("Successfully selected CMZ button on startup");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR during initial button selection: {ex.Message}");
                }
            };
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                // Update selected tab styling
                foreach (var child in TabsPanel.Children)
                {
                    if (child is Button b)
                    {
                        b.BorderThickness = new Thickness(0);
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
            try
            {
                if (sender is Button clickedButton && clickedButton.Tag is string tag)
                {
                    Debug.WriteLine($"Clicked sidebar button: {tag}");
                    
                    // Don't do anything if it's already selected
                    if (clickedButton == currentlySelectedSidebarButton)
                    {
                        Debug.WriteLine("Button already selected, no change needed");
                        return;
                    }
                    
                    // Clear ALL sidebar buttons (both in SidebarPanel and BottomSidebarPanel)
                    ClearAllSidebarButtons();
                    
                    // Update the clicked button
                    clickedButton.Background = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                    clickedButton.Foreground = Brushes.White;
                    currentlySelectedSidebarButton = clickedButton;
                    
                    // Store current selection if it's a mod (CMZ or CMW)
                    if (tag == "CMZ" || tag == "CMW")
                    {
                        currentSidebarSelection = tag;
                        
                        // Update the title text
                        if (tag == "CMZ")
                            EditionTitleText.Text = "CASTLEMINER Z";
                        else if (tag == "CMW")
                            EditionTitleText.Text = "CM WARFARE";
                    }
                    
                    NavigateToContent(tag);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR during sidebar button click: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Button Selection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ClearAllSidebarButtons()
        {
            // Clear game buttons in the main sidebar panel
            foreach (var child in SidebarPanel.Children)
            {
                if (child is Button button)
                {
                    button.Background = Brushes.Transparent;
                    button.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                }
            }
            
            // Clear utility buttons in the bottom sidebar panel
            foreach (var child in BottomSidebarPanel.Children)
            {
                if (child is Button button)
                {
                    button.Background = Brushes.Transparent;
                    button.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                }
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
            // Find the button with matching tag in the tabs panel
            foreach (var child in TabsPanel.Children)
            {
                if (child is Button button && button.Tag?.ToString() == tag)
                {
                    // Reset all tab buttons
                    foreach (var tabChild in TabsPanel.Children)
                    {
                        if (tabChild is Button b)
                        {
                            b.BorderThickness = new Thickness(0);
                        }
                    }
                    
                    // Select this tab button
                    button.BorderThickness = new Thickness(0, 0, 0, 2);
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    break;
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