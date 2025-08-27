using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMLauncher
{
	public class SteamGuardPromptWindow : Window
	{
		public string? GuardCode { get; private set; }

		public SteamGuardPromptWindow(string? hint = null)
		{
			Title = "Steam Guard";
			Owner = Application.Current?.MainWindow;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			SizeToContent = SizeToContent.WidthAndHeight;
			ResizeMode = ResizeMode.NoResize;
			Background = new SolidColorBrush(Color.FromRgb(27, 27, 27));
			Foreground = Brushes.White;

			var panel = new StackPanel { Margin = new Thickness(16) };
			panel.Children.Add(new TextBlock
			{
				Text = hint ?? "Enter your Steam Guard code:",
				Margin = new Thickness(0, 0, 0, 8),
				TextWrapping = TextWrapping.Wrap
			});
			var box = new TextBox { Width = 240 }; box.Focus();
			panel.Children.Add(box);

			var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
			var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
			cancel.Click += (s, e) => { DialogResult = false; Close(); };
			var ok = new Button { Content = "OK", Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
			ok.Click += (s, e) =>
			{
				GuardCode = box.Text?.Trim();
				DialogResult = !string.IsNullOrWhiteSpace(GuardCode);
				if (DialogResult == true) Close();
			};
			buttons.Children.Add(cancel);
			buttons.Children.Add(ok);
			panel.Children.Add(buttons);

			Content = panel;
		}
	}
}
