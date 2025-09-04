using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CMLauncher
{
	// Simple progress dialog created in code-behind (no XAML) to avoid extra files
	internal sealed class DownloadProgressWindow : Window
	{
		private readonly ProgressBar _bar;
		private readonly TextBlock _status;

		public DownloadProgressWindow(string title)
		{
			Title = title;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			ResizeMode = ResizeMode.NoResize;
			SizeToContent = SizeToContent.WidthAndHeight;
			Background = new SolidColorBrush(Color.FromRgb(27, 27, 27));
			Foreground = Brushes.White;
			Owner = Application.Current?.MainWindow;

			var root = new StackPanel { Margin = new Thickness(20), MinWidth = 420 };
			_status = new TextBlock { Text = "Preparing...", Margin = new Thickness(0, 0, 0, 10) };
			_bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 16, Width = 380, IsIndeterminate = true };

			root.Children.Add(_status);
			root.Children.Add(_bar);
			Content = root;
		}

		public void SetIndeterminate(bool value)
		{
			if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => SetIndeterminate(value)); return; }
			_bar.IsIndeterminate = value;
		}

		public void UpdateProgress(double percent, string? message = null)
		{
			if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => UpdateProgress(percent, message)); return; }
			_bar.IsIndeterminate = false;
			_bar.Value = Math.Clamp(percent, 0, 100);
			if (!string.IsNullOrWhiteSpace(message))
			{
				_status.Text = message;
			}
		}

		public void UpdateStatus(string message)
		{
			if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => UpdateStatus(message)); return; }
			_status.Text = message;
		}
	}
}
