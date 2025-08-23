using System;
using System.Windows;
using System.Windows.Controls;

namespace CMLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Set default content
            NavigateToContent("CMZ");
            InstanceComboBox.SelectedIndex = 0;
        }

        private void SidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                NavigateToContent(tag);
            }
        }

        private void NavigateToContent(string contentTag)
        {
            // Navigate to the specific content page
            MainContentFrame.Navigate(new ModContentPage(contentTag));
            SelectedModTextBlock.Text = $"Selected: {contentTag}";
        }

        private void InstanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InstanceComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Handle instance selection
                string instanceName = selectedItem.Content.ToString() ?? "Unknown";
                // You would typically load the instance configuration here
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle launching the game/mod
            MessageBox.Show("Launching game...", "CM Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // In a real implementation, you would:
            // 1. Check for updates
            // 2. Verify game files
            // 3. Launch the appropriate executable with correct parameters
        }
    }
}