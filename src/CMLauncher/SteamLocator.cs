using Microsoft.Win32;
using System.IO;

namespace CMLauncher
{
	public static class SteamLocator
	{
		public static string? FindGamePath(string appId)
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(@"Software\\Valve\\Steam");
				var steamPath = key?.GetValue("SteamPath") as string;
				if (string.IsNullOrWhiteSpace(steamPath)) return null;
				var libraryVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
				if (!File.Exists(libraryVdf)) return null;

				foreach (var line in File.ReadAllLines(libraryVdf))
				{
					// crude parse: lines like "1"  "D:\\SteamLibrary"
					if (!(line.Contains(":\\") || line.Contains(":/"))) continue;
					var parts = line.Split('"');
					if (parts.Length < 4) continue;
					var path = parts[3].Replace("\\\\", "\\");
					var manifest = Path.Combine(path, "steamapps", $"appmanifest_{appId}.acf");
					if (!File.Exists(manifest)) continue;

					foreach (var mline in File.ReadAllLines(manifest))
					{
						if (mline.Contains("installdir"))
						{
							var mparts = mline.Split('"');
							if (mparts.Length < 4) break;
							var installDir = mparts[3];
							var dir = Path.Combine(path, "steamapps", "common", installDir);
							if (Directory.Exists(dir)) return dir;
						}
					}
				}
			}
			catch { }
			return null;
		}
	}
}
