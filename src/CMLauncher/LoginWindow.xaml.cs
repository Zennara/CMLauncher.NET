using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

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
			ShowActivity("Logging in...");

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
			const string RequiredSuccessFragment = "licenses for account!"; // CHANGE ME if needed
			bool sawRequiredOutput = false;
			bool invalidPassword = false;
			bool rateLimited = false;
			bool emailPromptShown = false;
			bool codeWrong = false;
			bool promptOpen = false;
			object promptLock = new();
			bool killRequested = false;
			bool inputCheckEnabled = false; // enable stall-based prompt only after we see the login line
			bool mobileGuardDetected = false; // suppress email prompt when mobile confirmation is requested

			CancellationTokenSource stallCts = new CancellationTokenSource();

			async Task PromptForEmailCodeAsync()
			{
				// Suppress prompts once a wrong code was detected or mobile guard requested
				if (codeWrong || mobileGuardDetected) return;
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
						ShowActivity("Enter code from email...");
						var code = ShowInputDialog("Steam Guard Code", "Enter the Steam Guard code sent to your email:");
						if (!string.IsNullOrWhiteSpace(code))
						{
							ShowActivity("Checking 2FA code...");
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
				// Do not run stall-based checks until login line appears, or when mobile guard is requested
				if (!inputCheckEnabled || mobileGuardDetected) return;
				stallCts.Cancel();
				stallCts = new CancellationTokenSource();
				var token = stallCts.Token;

				Task.Run(async () =>
				{
					try
					{
						await Task.Delay(5000, token);
						if (!token.IsCancellationRequested)
						{
							// Only prompt once and not after wrong code or mobile guard
							if (!emailPromptShown && !codeWrong && !mobileGuardDetected)
							{
								await PromptForEmailCodeAsync();
							}
						}
					}
					catch (TaskCanceledException) { /* ignored */ }
				});
			}

			// NOTE: do not start stall timer until we know we are in login phase
			// ResetStallTimer();

			void HandleLine(string line)
			{
				outputLines.Add(line);
				Debug.WriteLine(line);

				// Enable stall-based input prompt only after we see the login message
				var lower = line.ToLowerInvariant();
				if (!inputCheckEnabled && lower.Contains("logging '") && lower.Contains(" into steam3"))
				{
					inputCheckEnabled = true;
					ResetStallTimer();
				}
				else if (inputCheckEnabled)
				{
					ResetStallTimer();
				}

				if (line.IndexOf(RequiredSuccessFragment, StringComparison.OrdinalIgnoreCase) >= 0)
					sawRequiredOutput = true;

				// Detect explicit Mobile App confirmation message: suppress email prompts
				if (!mobileGuardDetected && (lower.Contains("use the steam mobile app") || lower.Contains("steam mobile app") || (lower.Contains("mobile") && lower.Contains("confirm your sign in"))))
				{
					mobileGuardDetected = true;
					Dispatcher.Invoke(() =>
					{
						StatusText.Text = "Please accept the login request with Steam Guard";
						StatusText.Visibility = Visibility.Visible;
						ShowActivity("Waiting for Steam Guard...");
					});
				}

				// Steam Guard generic informational text
				if (!mobileGuardDetected && line.IndexOf("Steam Guard", StringComparison.OrdinalIgnoreCase) >= 0 &&
					line.IndexOf("auth code", StringComparison.OrdinalIgnoreCase) < 0)
				{
					Dispatcher.Invoke(() =>
					{
						StatusText.Text = "Please accept the login request with Steam Guard";
						StatusText.Visibility = Visibility.Visible;
						ShowActivity("Waiting for Steam Guard...");
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
				else if (!codeWrong && !mobileGuardDetected && line.IndexOf("Please enter the auth code", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Dispatcher.Invoke(() => ShowActivity("Enter code from email..."));
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

			// Failure UI
			HideActivity();
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

			StatusText.Visibility = Visibility.Collapsed;
			LoginButton.IsEnabled = true;
		}

		private void ShowActivity(string message)
		{
			if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => ShowActivity(message)); return; }
			var sb = TryFindResource("SpinnerStoryboard") as Storyboard;
			ActivityText.Visibility = Visibility.Visible;
			ActivityText.Text = message;
			ActivitySpinner.Visibility = Visibility.Visible;
			// Begin storyboard using ActivitySpinner as the containing namescope so TargetName resolves
			sb?.Begin(ActivitySpinner, isControllable: true);
		}

		private void HideActivity()
		{
			if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(HideActivity); return; }
			var sb = TryFindResource("SpinnerStoryboard") as Storyboard;
			// Stop against the same element used to start
			sb?.Stop(ActivitySpinner);
			ActivitySpinner.Visibility = Visibility.Collapsed;
			ActivityText.Visibility = Visibility.Collapsed;
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
