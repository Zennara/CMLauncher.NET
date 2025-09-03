using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CMLauncher
{
	public partial class LoginWindow : Window
	{
		private bool _rateLimitPopupShown;

		public LoginWindow()
		{
			InitializeComponent();
			UsernameBox.Text = LauncherSettings.Current.SteamUsername ?? string.Empty;
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			var u = UsernameBox.Text?.Trim() ?? "";
			var p = PasswordBox.Password ?? "";

			if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
			{
				MessageBox.Show(this, "Please enter username and password.", "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Prevent double-clicks while processing
			LoginButton.IsEnabled = false;

			Debug.WriteLine("Starting DepotDownloader login test...");

			var path = "depot-downloader/DepotDownloader.exe";
			var args = $"-app 253430 -depot 253431 -username {u} -password {p} -manifest-only";

			var outputLines = new List<string>();
			var process = new Process();
			process.StartInfo.FileName = path;
			process.StartInfo.Arguments = args;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;

			// Configure success/failure markers
			// TODO: Change this to the exact success line printed by your DepotDownloader build
			const string RequiredSuccessFragment = "Done!"; // CHANGE ME if needed
			bool sawRequiredOutput = false;
			bool invalidPassword = false;
			bool rateLimited = false;
			bool emailPromptShown = false;
			bool codeWrong = false;
			bool promptOpen = false;
			object promptLock = new();
			bool killRequested = false;

			CancellationTokenSource stallCts = new CancellationTokenSource();

			async Task PromptForEmailCodeAsync()
			{
				// Suppress prompts once a wrong code was detected
				if (codeWrong) return;
				lock (promptLock)
				{
					if (promptOpen || emailPromptShown) return;
					promptOpen = true;
					emailPromptShown = true;
				}
				try
				{
					await Dispatcher.InvokeAsync(async () =>
					{
						var code = ShowInputDialog("Steam Guard Code", "Enter the Steam Guard code sent to your email:");
						if (!string.IsNullOrWhiteSpace(code))
						{
							try { await process.StandardInput.WriteLineAsync(code); } catch { }
						}
					});
				}
				finally
				{
					lock (promptLock) { promptOpen = false; }
				}
			}

			void ResetStallTimer()
			{
				stallCts.Cancel();
				stallCts = new CancellationTokenSource();
				var token = stallCts.Token;

				Task.Run(async () =>
				{
					try
					{
						await Task.Delay(3000, token);
						if (!token.IsCancellationRequested)
						{
							// Only prompt once and not after wrong code
							if (!emailPromptShown && !codeWrong)
							{
								await PromptForEmailCodeAsync();
							}
						}
					}
					catch (TaskCanceledException) { /* ignored */ }
				});
			}

			ResetStallTimer();

			void HandleLine(string line)
			{
				outputLines.Add(line);
				Debug.WriteLine(line);
				ResetStallTimer();

				if (line.IndexOf(RequiredSuccessFragment, StringComparison.OrdinalIgnoreCase) >= 0)
					sawRequiredOutput = true;

				// Steam Guard mobile informational text
				if (line.IndexOf("Steam Guard", StringComparison.OrdinalIgnoreCase) >= 0 &&
					line.IndexOf("auth code", StringComparison.OrdinalIgnoreCase) < 0)
				{
					Dispatcher.Invoke(() =>
					{
						StatusText.Text = "Waiting for Steam Guard Mobile...";
						StatusText.Visibility = Visibility.Visible;
					});
				}

				if (line.IndexOf("InvalidPassword", StringComparison.OrdinalIgnoreCase) >= 0 ||
					line.IndexOf("Invalid Password", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					invalidPassword = true;
				}
				else if (line.IndexOf("RateLimit", StringComparison.OrdinalIgnoreCase) >= 0 ||
					line.IndexOf("rate limit", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					rateLimited = true;
				}
				else if ((line.IndexOf("2-factor", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("two-factor", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("2fa", StringComparison.OrdinalIgnoreCase) >= 0)
					&& line.IndexOf("incorrect", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					// Wrong 2FA: flag and terminate the process immediately
					codeWrong = true;
					try { stallCts.Cancel(); } catch { }
					if (!killRequested)
					{
						killRequested = true;
						try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
					}
				}
				else if (!codeWrong && line.IndexOf("Please enter the auth code", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					// Immediate prompt when process asks for code (only once)
					_ = PromptForEmailCodeAsync();
				}
			}

			process.OutputDataReceived += (_, e2) =>
			{
				if (string.IsNullOrEmpty(e2.Data)) return;
				HandleLine("[OUT] " + e2.Data);
			};

			process.ErrorDataReceived += (_, e2) =>
			{
				if (string.IsNullOrEmpty(e2.Data)) return;
				HandleLine("[ERR] " + e2.Data);
			};

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await process.WaitForExitAsync();
			stallCts.Cancel();

			Debug.WriteLine($"DepotDownloader finished with exit code {process.ExitCode}");
			Debug.WriteLine($"Total lines captured: {outputLines.Count}");

			// Only succeed if exit code is 0 and we saw the required output
			if (process.ExitCode == 0 && sawRequiredOutput && !invalidPassword && !rateLimited && !codeWrong)
			{
				DialogResult = true;
				LauncherSettings.Current.SteamUsername = u;
				LauncherSettings.Current.SteamPassword = p;
				LauncherSettings.Current.Save();
				Close();
				return;
			}

			// Failure: inform user and allow retry
			if (invalidPassword)
			{
				MessageBox.Show(this, "Invalid username or password. Please try again.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else if (rateLimited)
			{
				MessageBox.Show(this, "Too many login attempts. Please wait and try again later.", "Rate Limited", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			else if (codeWrong)
			{
				MessageBox.Show(this, "Invalid 2FA code. Please re-login and try again.", "Invalid Code", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else
			{
				MessageBox.Show(this, "An unknown error occurred while logging in. ", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
			}

			// Reset UI for retry
			StatusText.Visibility = Visibility.Collapsed;
			LoginButton.IsEnabled = true;
		}

		private string? ShowInputDialog(string title, string message)
		{
			var dialog = new Window
			{
				Title = title,
				Owner = this,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				SizeToContent = SizeToContent.WidthAndHeight,
				ResizeMode = ResizeMode.NoResize
			};

			var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
			grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });

			var textBlock = new System.Windows.Controls.TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap, Width = 320 };
			System.Windows.Controls.Grid.SetRow(textBlock, 0);
			grid.Children.Add(textBlock);

			var input = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 12) };
			System.Windows.Controls.Grid.SetRow(input, 1);
			grid.Children.Add(input);

			var buttonPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
			var okButton = new System.Windows.Controls.Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
			var cancelButton = new System.Windows.Controls.Button { Content = "Cancel", Width = 80, IsCancel = true };
			okButton.Click += (_, __) => dialog.DialogResult = true;
			cancelButton.Click += (_, __) => dialog.DialogResult = false;
			buttonPanel.Children.Add(okButton);
			buttonPanel.Children.Add(cancelButton);
			System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
			grid.Children.Add(buttonPanel);

			dialog.Content = grid;
			dialog.Loaded += (_, __) => input.Focus();
			var result = dialog.ShowDialog();
			return result == true ? input.Text?.Trim() : null;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			if (DialogResult != true)
			{
				try { Application.Current.Shutdown(0); } catch { }
				System.Environment.Exit(0);
			}
		}
	}
}
