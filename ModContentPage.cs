using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class ModContentPage : Page
    {
        public ModContentPage(string modType)
        {
            // Create UI for the specific mod type
            var panel = new StackPanel
            {
                Margin = new Thickness(20)
            };
            
            panel.Children.Add(new TextBlock
            {
                Text = $"{modType} Content",
                FontSize = 24,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.White
            });
            
            // Add mod-specific content
            if (modType == "CMZ")
            {
                AddCMZContent(panel);
            }
            else if (modType == "CMW")
            {
                AddCMWContent(panel);
            }
            else if (modType == "Settings")
            {
                AddSettingsContent(panel);
            }
            
            Content = panel;
        }
        
        private void AddCMZContent(StackPanel panel)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Welcome to CMZ mod manager",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.LightGray
            });
            
            // Add more CMZ-specific content here
        }
        
        private void AddCMWContent(StackPanel panel)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Welcome to CMW mod manager",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.LightGray
            });
            
            // Add more CMW-specific content here
        }
        
        private void AddSettingsContent(StackPanel panel)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Launcher Settings",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.LightGray
            });
            
            // Add settings options here
            var gamePathPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 5) };
            gamePathPanel.Children.Add(new TextBlock 
            { 
                Text = "Game Path:", 
                Width = 120,
                Foreground = Brushes.LightGray 
            });
            gamePathPanel.Children.Add(new TextBox 
            { 
                Width = 300, 
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80))
            });
            
            panel.Children.Add(gamePathPanel);
            
            // Add more settings options here
        }
    }
}