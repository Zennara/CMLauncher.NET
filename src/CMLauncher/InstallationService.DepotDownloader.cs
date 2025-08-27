using System;
using System.Diagnostics;
using System.IO;
using System.Text;

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
	}
}
