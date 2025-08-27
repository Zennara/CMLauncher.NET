using System.IO;
using System.Text.Json;

namespace CMLauncher
{
	public class LauncherSettings
	{
		public bool CloseOnLaunch { get; set; } = false;
		public string? SteamPathCMZ { get; set; }
		public string? SteamPathCMW { get; set; }

		// Saved DepotDownloader credentials
		public string? SteamUsername { get; set; }
		public string? SteamPassword { get; set; }
		public bool? OwnsCMZ { get; set; }
		public bool? OwnsCMW { get; set; }

		// Remember last selected installation per game (store the display name e.g., "Steam Installation" or custom name)
		public string? LastSelectedCMZ { get; set; }
		public string? LastSelectedCMW { get; set; }

		private static LauncherSettings? _current;
		public static LauncherSettings Current
		{
			get
			{
				_current ??= Load();
				return _current!;
			}
			set => _current = value;
		}

		public static string GetSettingsPath()
		{
			var root = InstallationService.GetRootPath();
			Directory.CreateDirectory(root);
			return Path.Combine(root, "launcher-settings.json");
		}

		public static LauncherSettings Load()
		{
			try
			{
				var path = GetSettingsPath();
				if (File.Exists(path))
				{
					var json = File.ReadAllText(path);
					var s = JsonSerializer.Deserialize<LauncherSettings>(json);
					if (s != null) return s;
				}
			}
			catch { }
			return new LauncherSettings();
		}

		public void Save()
		{
			try
			{
				var path = GetSettingsPath();
				var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(path, json);
			}
			catch { }
		}

		public string? GetSteamPathForGame(string gameKey)
		{
			if (string.Equals(gameKey, InstallationService.CMWKey, StringComparison.OrdinalIgnoreCase)) return SteamPathCMW;
			return SteamPathCMZ;
		}

		public string? GetLastSelectedInstallation(string gameKey)
		{
			return string.Equals(gameKey, InstallationService.CMWKey, System.StringComparison.OrdinalIgnoreCase)
				? LastSelectedCMW
				: LastSelectedCMZ;
		}

		public void SetLastSelectedInstallation(string gameKey, string? name)
		{
			if (string.Equals(gameKey, InstallationService.CMWKey, System.StringComparison.OrdinalIgnoreCase))
				LastSelectedCMW = name;
			else
				LastSelectedCMZ = name;
		}
	}
}
