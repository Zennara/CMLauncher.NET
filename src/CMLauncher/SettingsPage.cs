using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
    public class SettingsPage : Page
    {
        private Brush GetAccentBrush()
        {
            var fallback = new SolidColorBrush(Color.FromRgb(183, 28, 28)); // #B71C1C
            try
            {
                if (Application.Current?.MainWindow != null && Application.Current.MainWindow.Resources.Contains("AccentBrush"))
                    return (Brush)Application.Current.MainWindow.Resources["AccentBrush"];
                if (Application.Current?.Resources != null && Application.Current.Resources.Contains("AccentBrush"))
                    return (Brush)Application.Current.Resources["AccentBrush"];
            }
            catch { }
            return fallback;
        }

        public SettingsPage()
        {
            var scrollViewer = new ScrollViewer();
            var panel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
                Margin = new Thickness(20)
            };
            
            panel.Children.Add(new TextBlock
            {
                Text = "Launcher Settings",
                FontSize = 24,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.White
            });
            
            // Game Directory
            AddSettingsSection(panel, "Game Directory");
            
            var gamePathPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 5) };
            gamePathPanel.Children.Add(new TextBox 
            { 
                Width = 400, 
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                Text = "C:\\Games\\CM"
            });
            gamePathPanel.Children.Add(new Button
            {
                Content = "Browse",
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            });
            panel.Children.Add(gamePathPanel);
            
            // Allocated Memory
            AddSettingsSection(panel, "Allocated Memory");
            
            var memoryPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 20) };
            var slider = new Slider
            {
                Width = 300,
                Minimum = 2,
                Maximum = 16,
                Value = 8,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center
            };
            memoryPanel.Children.Add(slider);
            memoryPanel.Children.Add(new TextBlock
            {
                Text = "8 GB",
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            });
            panel.Children.Add(memoryPanel);
            
            // Java Settings
            AddSettingsSection(panel, "Java Settings");
            
            var javaPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 20) };
            
            var javaPathPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            javaPathPanel.Children.Add(new TextBlock
            {
                Text = "Java Path:",
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.LightGray
            });
            javaPathPanel.Children.Add(new TextBox
            {
                Width = 300,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                Text = "C:\\Program Files\\Java\\jre1.8.0_301\\bin\\javaw.exe"
            });
            javaPathPanel.Children.Add(new Button
            {
                Content = "Browse",
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(10, 2, 10, 2),
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            });
            javaPanel.Children.Add(javaPathPanel);
            
            var javaArgsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            javaArgsPanel.Children.Add(new TextBlock
            {
                Text = "JVM Args:",
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.LightGray
            });
            javaArgsPanel.Children.Add(new TextBox
            {
                Width = 300,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                Text = "-Xmx8G -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC"
            });
            javaPanel.Children.Add(javaArgsPanel);
            
            panel.Children.Add(javaPanel);
            
            // Save button
            var saveButton = new Button
            {
                Content = "Save Settings",
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(20, 10, 20, 10),
                Background = GetAccentBrush(),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 20, 0, 0)
            };
            panel.Children.Add(saveButton);
            
            scrollViewer.Content = panel;
            Content = scrollViewer;
        }
        
        private void AddSettingsSection(StackPanel panel, string title)
        {
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 15, 0, 5),
                Foreground = Brushes.White
            });
            
            panel.Children.Add(new Separator
            {
                Background = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                Margin = new Thickness(0, 5, 0, 5)
            });
        }
    }
}