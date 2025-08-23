using System;
using System.Diagnostics;
using System.Linq;
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
        private ComboBox InstanceComboBox;
        private string currentSidebarSelection = "CMZ"; // Track current sidebar selection
        private Button currentlySelectedSidebarButton; // Keep track of the currently selected sidebar button
        private Button _selectedInstallButton;

        private readonly SolidColorBrush DefaultForegroundBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204));

        public MainWindow()
        {
            InitializeComponent();
            
            InstanceComboBox = FindName("InstanceComboBox") as ComboBox;
            
            NavigateToContent("CMZ");
            
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

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                foreach (var child in TabsPanel.Children)
                {
                    if (child is Button b) b.BorderThickness = new Thickness(0);
                }
                
                button.BorderThickness = new Thickness(0, 0, 0, 2);
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                
                switch (tag)
                {
                    case "Play":
                        NavigateToContent(currentSidebarSelection);
                        break;
                    case "Installations":
                        MainContentFrame.Navigate(new InstallationsPage());
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

        private void NavigateToContent(string contentTag)
        {
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

            if (contentTag == "CMZ" || contentTag == "CMW")
            {
                SelectTabButton("Play");
            }
        }
        
        private void SelectTabButton(string tag)
        {
            foreach (var child in TabsPanel.Children)
            {
                if (child is Button b)
                {
                    if (b.Tag?.ToString() == tag)
                    {
                        b.BorderThickness = new Thickness(0, 0, 0, 2);
                        b.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
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
    }
}