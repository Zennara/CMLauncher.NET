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
		private readonly TextBox _log;

		public DownloadProgressWindow(string title)
		{
			Title = title;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			ResizeMode = ResizeMode.NoResize;
			SizeToContent = SizeToContent.Manual; // fixed size
			Width = 640;
			Height = 420;
			Background = new SolidColorBrush(Color.FromRgb(27, 27, 27));
			Foreground = Brushes.White;
			Owner = Application.Current?.MainWindow;

			var root = new StackPanel { Margin = new Thickness(20), Width = 600 };
			_status = new TextBlock { Text = "Preparing...", Margin = new Thickness(20, 0, 0, 10) };
			_bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 16, Width = 560, IsIndeterminate = true };

			// Console-like log area
			_log = new TextBox
			{
				Margin = new Thickness(0, 10, 0, 0),
				Width = 560,
				Height = 260,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				TextWrapping = TextWrapping.NoWrap,
				IsReadOnly = true,
				Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
				Foreground = Brushes.Gainsboro,
				BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
				FontFamily = new FontFamily("Consolas"),
				FontSize = 12
			};

			root.Children.Add(_status);
			root.Children.Add(_bar);
			root.Children.Add(_log);
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

		public void AppendLog(string line)
		{
			if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => AppendLog(line)); return; }
			if (string.IsNullOrEmpty(line)) return;
			_log.AppendText(line + Environment.NewLine);
			_log.CaretIndex = _log.Text.Length;
			_log.ScrollToEnd();
		}
	}
}
