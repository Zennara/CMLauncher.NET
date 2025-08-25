using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace CMLauncher
{
	public class HomePage : Page
	{
		public HomePage()
		{
			var panel = new StackPanel
			{
				Background = new SolidColorBrush(Color.FromRgb(32, 32, 32)),
				Margin = new Thickness(20)
			};

			panel.Children.Add(new TextBlock
			{
				Text = "Welcome to CastleMiner Launcher",
				FontSize = 24,
				Margin = new Thickness(0, 0, 0, 20),
				Foreground = Brushes.White
			});

			panel.Children.Add(new TextBlock
			{
				Text = "Select a game from the sidebar to play",
				FontSize = 16,
				Margin = new Thickness(0, 0, 0, 20),
				Foreground = Brushes.LightGray
			});

			Content = panel;
		}
	}
}