using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CMLauncher
{
	public class DepotDownloaderTerminalWindow : Window
	{
		private readonly TextBox _output;
		private readonly TextBox _input;
		private Process? _proc;

		public DepotDownloaderTerminalWindow()
		{
			Title = "DepotDownloader Terminal";
			Width = 900;
			Height = 540;
			Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
			Foreground = Brushes.White;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			var root = new Grid { Margin = new Thickness(8) };
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			_output = new TextBox
			{
				IsReadOnly = true,
				AcceptsReturn = true,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				Background = new SolidColorBrush(Color.FromRgb(15, 15, 15)),
				Foreground = Brushes.White,
				FontFamily = new FontFamily("Consolas"),
				TextWrapping = TextWrapping.Wrap
			};
			Grid.SetRow(_output, 0);
			root.Children.Add(_output);

			var inputPanel = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
			_input = new TextBox
			{
				MinWidth = 400,
				FontFamily = new FontFamily("Consolas"),
				Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
				Foreground = Brushes.White
			};
			_input.KeyDown += (s, e) =>
			{
				if (e.Key == Key.Enter)
				{
					SendInputLine();
					e.Handled = true;
				}
			};
			var sendBtn = new Button { Content = "Send", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(6, 0, 0, 0) };
			sendBtn.Click += (s, e) => SendInputLine();
			DockPanel.SetDock(sendBtn, Dock.Right);
			inputPanel.Children.Add(sendBtn);
			inputPanel.Children.Add(_input);
			Grid.SetRow(inputPanel, 1);
			root.Children.Add(inputPanel);

			Content = root;

			Closed += (_, __) =>
			{
				try { if (!_proc?.HasExited ?? false) _proc!.Kill(true); } catch { }
			};
		}

		public void Start(string exePath, string args, string workingDir)
		{
			try
			{
				_proc = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = exePath,
						Arguments = args,
						WorkingDirectory = workingDir,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						RedirectStandardInput = true,
						CreateNoWindow = true
					},
					EnableRaisingEvents = true
				};
				_proc.OutputDataReceived += OnProcData;
				_proc.ErrorDataReceived += OnProcData;
				_proc.Start();
				_proc.BeginOutputReadLine();
				_proc.BeginErrorReadLine();
			}
			catch (Exception ex)
			{
				Append($"Failed to start DepotDownloader: {ex.Message}\n");
			}
		}

		private void OnProcData(object sender, DataReceivedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.Data)) return;
			Dispatcher.Invoke(() => Append(e.Data + "\n"));
			try { OutputReceived?.Invoke(e.Data!); } catch { }
		}

		private void Append(string text)
		{
			_output.AppendText(text);
			_output.ScrollToEnd();
		}

		private void SendInputLine()
		{
			try
			{
				var line = _input.Text ?? string.Empty;
				_input.Clear();
				_proc?.StandardInput.WriteLine(line);
			}
			catch { }
		}

		public void SendLine(string line)
		{
			try { _proc?.StandardInput.WriteLine(line); } catch { }
		}

		public event Action<string>? OutputReceived;
	}
}
