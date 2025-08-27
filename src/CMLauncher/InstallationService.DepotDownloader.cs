using System;
using System.Diagnostics;
using System.IO;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		public static string? GetDepotDownloaderExePath()
		{
			try
			{
				var ddExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "depot-downloader", "DepotDownloader.exe");
				if (!File.Exists(ddExe))
				{
					ddExe = Path.Combine(Directory.GetCurrentDirectory(), "depot-downloader", "DepotDownloader.exe");
				}
				return File.Exists(ddExe) ? ddExe : null;
			}
			catch { return null; }
		}

		// Simple external console window (most reliable approach)
		public static void OpenDepotDownloaderConsole(string appId, string depotId, string username, string password)
		{
			try
			{
				var ddExe = GetDepotDownloaderExePath();
				if (string.IsNullOrWhiteSpace(ddExe)) 
				{
					System.Windows.MessageBox.Show($"DepotDownloader.exe not found!\n\nExpected location:\n{ddExe ?? "Unknown"}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
					return;
				}

				var workingDir = Path.GetDirectoryName(ddExe) ?? AppDomain.CurrentDomain.BaseDirectory;
				var args = $"-app {appId} -depot {depotId} -manifest-only -username {username} -password \"{password}\"";
				
				// Try multiple approaches to ensure console opens
				
				// Approach 1: cmd.exe with /k to keep window open
				try
				{
					var psi1 = new ProcessStartInfo
					{
						FileName = "cmd.exe",
						Arguments = $"/k \"\"{ddExe}\" {args}\"",
						WorkingDirectory = workingDir,
						UseShellExecute = true,
						CreateNoWindow = false,
						WindowStyle = ProcessWindowStyle.Normal
					};
					Process.Start(psi1);
					return;
				}
				catch { }

				// Approach 2: Direct exe launch
				try
				{
					var psi2 = new ProcessStartInfo
					{
						FileName = ddExe,
						Arguments = args,
						WorkingDirectory = workingDir,
						UseShellExecute = true,
						CreateNoWindow = false,
						WindowStyle = ProcessWindowStyle.Normal
					};
					Process.Start(psi2);
					return;
				}
				catch { }

				System.Windows.MessageBox.Show("Failed to open DepotDownloader console window.", "Error");
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show($"Error opening DepotDownloader: {ex.Message}", "Error");
			}
		}
	}
}
