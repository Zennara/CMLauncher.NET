using System.ComponentModel;
using System.Diagnostics;
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

			LauncherSettings.Current.SteamUsername = u;
			LauncherSettings.Current.SteamPassword = p;

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

			CancellationTokenSource stallCts = new CancellationTokenSource();

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
							// Waiting for Steam Guard input from email.
						}
					}
					catch (TaskCanceledException) { /* ignored */ }
				});
			}

			ResetStallTimer();

			process.OutputDataReceived += (sender2, e2) =>
			{
				if (string.IsNullOrEmpty(e2.Data)) return;

				outputLines.Add(e2.Data);
				Debug.WriteLine("[OUT] " + e2.Data);

				ResetStallTimer();

				if (e2.Data.Contains("Steam Guard", StringComparison.OrdinalIgnoreCase))
				{
					// Steam Guard mobile detected,
				}

				// TODO: Other checks like invalid password, rate limit, success, etc.
			};

			process.ErrorDataReceived += (sender2, e2) =>
			{
				if (string.IsNullOrEmpty(e2.Data)) return;

				outputLines.Add(e2.Data);
				Debug.WriteLine("[ERR] " + e2.Data);

				ResetStallTimer();
			};

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await process.WaitForExitAsync();

			Debug.WriteLine($"DepotDownloader finished with exit code {process.ExitCode}");
			Debug.WriteLine($"Total lines captured: {outputLines.Count}");

			DialogResult = true;
			Close();
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
