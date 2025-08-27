using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace CMLauncher
{
	public static partial class InstallationService
	{
		public const string RootFolderName = ".castleminer";
		public const string CMZKey = "CMZ";
		public const string CMWKey = "CMW";
		private const string CMZAppId = "253430";
		private const string CMWAppId = "675210";

		public static string GetRootPath()
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			return Path.Combine(appData, RootFolderName);
		}

		public static string GetGameRoot(string gameKey)
		{
			var gameFolder = string.Equals(gameKey, CMWKey, StringComparison.OrdinalIgnoreCase) ? "cmw" : "cmz";
			return Path.Combine(GetRootPath(), gameFolder);
		}

		public static string GetInstallationsPath(string gameKey) => Path.Combine(GetGameRoot(gameKey), "installations");
		public static string GetVersionsPath(string gameKey) => Path.Combine(GetGameRoot(gameKey), "versions");
		public static string GetBlocksAssetsPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "blocks");

		public static void EnsureSteamAppId(string gameKey, string gameDir)
		{
			try
			{
				Directory.CreateDirectory(gameDir);
				var appId = string.Equals(gameKey, CMWKey, StringComparison.OrdinalIgnoreCase) ? CMWAppId : CMZAppId;
				var file = Path.Combine(gameDir, "steam_appid.txt");
				if (!File.Exists(file) || (File.ReadAllText(file).Trim() != appId))
				{
					File.WriteAllText(file, appId);
				}
			}
			catch { }
		}

		public static void EnsureDirectoryStructure()
		{
			foreach (var key in new[] { CMZKey, CMWKey })
			{
				Directory.CreateDirectory(GetInstallationsPath(key));
				Directory.CreateDirectory(GetVersionsPath(key));
			}
		}

		public static List<InstallationInfo> LoadInstallations(string gameKey)
		{
			var list = new List<InstallationInfo>();
			var path = GetInstallationsPath(gameKey);
			if (!Directory.Exists(path))
				return list;

			foreach (var dir in Directory.GetDirectories(path))
			{
				var name = Path.GetFileName(dir);
				string version = "Unknown";
				DateTime? ts = null;
				string? icon = null;
				var infoFile = Path.Combine(dir, "installation-info.json");
				try
				{
					if (File.Exists(infoFile))
					{
						var json = File.ReadAllText(infoFile);
						var doc = JsonSerializer.Deserialize<InstallationInfoFile>(json);
						if (doc != null)
						{
							version = doc.version ?? version;
							if (!string.IsNullOrWhiteSpace(doc.timestamp) && DateTime.TryParse(doc.timestamp, out var dt))
								ts = dt;
							icon = doc.icon;
						}
					}
				}
				catch
				{
					// ignore malformed json
				}

				list.Add(new InstallationInfo
				{
					GameKey = gameKey,
					Name = name,
					Version = version,
					Timestamp = ts,
					RootPath = dir,
					IconName = icon
				});
			}

			// Sort by timestamp desc, then name
			list.Sort((a, b) =>
			{
				var t = Nullable.Compare(b.Timestamp, a.Timestamp);
				return t != 0 ? t : StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
			});
			return list;
		}

		public static List<string> LoadAvailableVersions(string gameKey)
		{
			var versions = new List<string>();
			var path = GetVersionsPath(gameKey);
			if (Directory.Exists(path))
			{
				foreach (var dir in Directory.GetDirectories(path))
				{
					versions.Add(Path.GetFileName(dir));
				}
			}
			versions.Sort(StringComparer.OrdinalIgnoreCase);
			return versions;
		}

		public static List<string> LoadAvailableIcons()
		{
			var blocksPath = GetBlocksAssetsPath();
			if (!Directory.Exists(blocksPath)) return new List<string>();
			var names = Directory.GetFiles(blocksPath)
				.Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
				.Select(Path.GetFileName)
				.Where(n => n != null)!
				.Cast<string>()
				.ToList();
			names.Sort(StringComparer.OrdinalIgnoreCase);
			return names;
		}

		public static string GetExeName(string gameKey) => string.Equals(gameKey, CMWKey, StringComparison.OrdinalIgnoreCase) ? "CastleMinerWarfare.exe" : "CastleMinerZ.exe";
		public static string GetAppId(string gameKey) => string.Equals(gameKey, CMWKey, StringComparison.OrdinalIgnoreCase) ? CMWAppId : CMZAppId;

		public static string EnsureVersionByManifest(string gameKey, string manifestId, string branch = "public")
		{
			var versionsRoot = GetVersionsPath(gameKey);
			Directory.CreateDirectory(versionsRoot);
			var target = Path.Combine(versionsRoot, manifestId);
			if (Directory.Exists(target)) return target;

			try
			{
				var ddExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "depot-downloader", "DepotDownloader.exe");
				if (!File.Exists(ddExe))
				{
					ddExe = Path.Combine(Directory.GetCurrentDirectory(), "depot-downloader", "DepotDownloader.exe");
				}
				if (File.Exists(ddExe))
				{
					var appId = GetAppId(gameKey);
					var depotId = gameKey == CMZKey ? "253431" : "675211";
					Directory.CreateDirectory(target);
					var branchArg = string.Equals(branch, "public", StringComparison.OrdinalIgnoreCase) ? string.Empty : $" -branch {branch}";
					var creds = BuildCredentialArgs();
					var psi = new ProcessStartInfo
					{
						FileName = ddExe,
						Arguments = $"-app {appId} -depot {depotId} -manifest {manifestId}{branchArg}{creds} -dir \"{target}\"",
						WorkingDirectory = Path.GetDirectoryName(ddExe) ?? AppDomain.CurrentDomain.BaseDirectory,
						UseShellExecute = true,
						CreateNoWindow = false
					};
					var p = Process.Start(psi);
					p!.WaitForExit();
				}
			}
			catch { }

			return target;
		}

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

		private static string BuildCredentialArgs(string username, string password)
		{
			return $" -username {username} -password \"{password}\"";
		}

		// Existing settings-based builder remains
		private static string BuildCredentialArgs()
		{
			var u = LauncherSettings.Current.SteamUsername;
			var p = LauncherSettings.Current.SteamPassword;
			if (!string.IsNullOrWhiteSpace(u) && !string.IsNullOrWhiteSpace(p))
			{
				return BuildCredentialArgs(u, p);
			}
			return string.Empty;
		}

		private static bool ContainsSteamGuardPrompt(string text)
		{
			if (string.IsNullOrEmpty(text)) return false;
			return text.Contains("STEAM GUARD", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Steam Guard", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Use the Steam Mobile App to confirm your sign in", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Please enter the 2-factor code", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Two-factor code", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Steam Guard code", StringComparison.OrdinalIgnoreCase);
		}

		private static (string output, bool steamGuard) RunDepotProbe(string appId, string depotId, string username, string password, Action? onSteamGuardDetected)
		{
			var ddExe = GetDepotDownloaderExePath();
			if (string.IsNullOrWhiteSpace(ddExe)) return (string.Empty, false);

			var creds = BuildCredentialArgs(username, password);
			var psi = new ProcessStartInfo
			{
				FileName = ddExe,
				Arguments = $"-app {appId} -depot {depotId}{creds} -manifest-only",
				WorkingDirectory = Path.GetDirectoryName(ddExe) ?? AppDomain.CurrentDomain.BaseDirectory,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			var sb = new StringBuilder();
			bool steamGuard = false;
			using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
			DataReceivedEventHandler onData = (_, e) =>
			{
				if (e.Data == null) return;
				sb.AppendLine(e.Data);
				if (!steamGuard && ContainsSteamGuardPrompt(e.Data))
				{
					steamGuard = true;
					try { onSteamGuardDetected?.Invoke(); } catch { }
					// Do NOT kill the process; let DepotDownloader continue listening for approval
				}
			};
			p.OutputDataReceived += onData;
			p.ErrorDataReceived += onData;

			try
			{
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();
			}
			catch { }

			return (sb.ToString(), steamGuard);
		}

		// Backward-compatible wrapper (no callback)
		private static (string output, bool steamGuard) RunDepotProbe(string appId, string depotId, string username, string password)
			=> RunDepotProbe(appId, depotId, username, password, null);

		public static (bool ownsCmz, bool ownsCmw, bool authOk, bool steamGuard) TryAuthenticateAndDetectOwnershipDetailed(string username, string password, Action? onSteamGuardDetected)
		{
			bool ownsCmz = false, ownsCmw = false, authOk = false, steamGuard = false;
			var ddExe = GetDepotDownloaderExePath();
			if (string.IsNullOrWhiteSpace(ddExe)) return (false, false, false, false);

			(string app, string depot)[] checks = new[] { (CMZAppId, "253431"), (CMWAppId, "675211") };
			foreach (var (app, depot) in checks)
			{
				try
				{
					var (output, sg) = RunDepotProbe(app, depot, username, password, onSteamGuardDetected);
					if (sg) { steamGuard = true; /* do not early-return; allow process to finish after approval */ }
					if (output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
					{
						authOk = false;
						return (false, false, false, steamGuard);
					}
					if (output.Contains("Got depot key", StringComparison.OrdinalIgnoreCase) || output.Contains("Processing depot", StringComparison.OrdinalIgnoreCase))
					{
						authOk = true;
						if (app == CMZAppId) ownsCmz = true; else ownsCmw = true;
					}
					else if (output.Contains("is not available from this account", StringComparison.OrdinalIgnoreCase))
					{
						authOk = true; // Auth worked but not owned
					}
				}
				catch { }
			}
			return (ownsCmz, ownsCmw, authOk, steamGuard);
		}

		// Existing method now delegates to overload with no callback
		public static (bool ownsCmz, bool ownsCmw, bool authOk, bool steamGuard) TryAuthenticateAndDetectOwnershipDetailed(string username, string password)
			=> TryAuthenticateAndDetectOwnershipDetailed(username, password, null);

		public static (bool authOk, bool steamGuard) TryAuthCredentialsWithCallback(string username, string password, Action? onSteamGuardDetected)
		{
			var (output, sg) = RunDepotProbe(CMZAppId, "253431", username, password, onSteamGuardDetected);
			if (output.Contains("Failed to authenticate", StringComparison.OrdinalIgnoreCase) || output.Contains("InvalidPassword", StringComparison.OrdinalIgnoreCase))
				return (false, sg);
			// If DepotDownloader is waiting for Steam Guard approval, sg == true; let caller handle UX.
			return (true, sg);
		}

		public static string? GetSteamInstallPath(string gameKey)
		{
			return LauncherSettings.Current.GetSteamPathForGame(gameKey) ?? SteamLocator.FindGamePath(GetAppId(gameKey));
		}

		public static string? GetSteamExePath(string gameKey)
		{
			var baseDir = GetSteamInstallPath(gameKey);
			if (string.IsNullOrWhiteSpace(baseDir)) return null;
			var exeName = GetExeName(gameKey);
			var path = Path.Combine(baseDir, exeName);
			return File.Exists(path) ? path : null;
		}

		public static string? GetSteamExeVersion(string gameKey)
		{
			try
			{
				var exe = GetSteamExePath(gameKey);
				if (string.IsNullOrWhiteSpace(exe)) return null;
				var info = FileVersionInfo.GetVersionInfo(exe);
				return !string.IsNullOrWhiteSpace(info.FileVersion) ? info.FileVersion : info.ProductVersion;
			}
			catch { return null; }
		}

		private static string? GetSteamMetaPath(string gameKey)
		{
			var dir = GetSteamInstallPath(gameKey);
			if (string.IsNullOrWhiteSpace(dir)) return null;
			return Path.Combine(dir, "cm_launcher_installation-info.json");
		}

		public static DateTime? GetSteamLastPlayed(string gameKey)
		{
			try
			{
				var meta = GetSteamMetaPath(gameKey);
				if (meta == null || !File.Exists(meta)) return null;
				var json = File.ReadAllText(meta);
				var doc = JsonSerializer.Deserialize<SteamInfoFile>(json);
				if (doc != null && !string.IsNullOrWhiteSpace(doc.timestamp) && DateTime.TryParse(doc.timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
				{
					return dt;
				}
			}
			catch { }
			return null;
		}

		public static void MarkSteamLaunched(string gameKey)
		{
			try
			{
				var meta = GetSteamMetaPath(gameKey);
				if (meta == null) return;
				var now = DateTime.UtcNow;
				var doc = new SteamInfoFile { timestamp = now.ToString("o") };
				var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(meta, json);
			}
			catch { }
		}

		public static InstallationInfo CreateInstallation(string gameKey, string name, string version, string? iconName = null)
		{
			var installsRoot = GetInstallationsPath(gameKey);
			Directory.CreateDirectory(installsRoot);

			string finalName = name;
			string candidatePath = Path.Combine(installsRoot, finalName);
			int i = 1;
			while (Directory.Exists(candidatePath))
			{
				finalName = $"{name} ({i++})";
				candidatePath = Path.Combine(installsRoot, finalName);
			}

			Directory.CreateDirectory(candidatePath);
			var gameDir = Path.Combine(candidatePath, "Game");
			Directory.CreateDirectory(gameDir);
			Directory.CreateDirectory(Path.Combine(candidatePath, "Data"));

			if (string.IsNullOrWhiteSpace(iconName))
			{
				var icons = LoadAvailableIcons();
				if (icons.Count > 0)
				{
					var rnd = new Random();
					iconName = icons[rnd.Next(icons.Count)];
				}
			}

			var versionSource = Path.Combine(GetVersionsPath(gameKey), version);
			if (string.Equals(version, "Steam Version", StringComparison.OrdinalIgnoreCase))
			{
				var appId = GetAppId(gameKey);
				var steamDir = SteamLocator.FindGamePath(appId);
				if (!string.IsNullOrWhiteSpace(steamDir) && Directory.Exists(steamDir))
				{
					versionSource = steamDir;
				}
			}
			else
			{
				var parts = version.Split('|');
				var manifest = parts[0];
				var branch = parts.Length > 1 ? parts[1] : "public";
				if (manifest.All(char.IsDigit))
				{
					versionSource = EnsureVersionByManifest(gameKey, manifest, branch);
				}
			}

			try
			{
				if (Directory.Exists(versionSource))
				{
					DirectoryCopy(versionSource, gameDir, true);
				}
			}
			catch { }

			var info = new InstallationInfo
			{
				GameKey = gameKey,
				Name = finalName,
				Version = version,
				Timestamp = null,
				RootPath = candidatePath,
				IconName = iconName
			};

			var infoFile = Path.Combine(candidatePath, "installation-info.json");
			var json2 = JsonSerializer.Serialize(new InstallationInfoFile
			{
				version = version,
				timestamp = null,
				icon = iconName
			}, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(infoFile, json2);

			EnsureSteamAppId(gameKey, gameDir);

			return info;
		}

		public static void DeleteInstallation(InstallationInfo info)
		{
			try
			{
				if (Directory.Exists(info.RootPath))
				{
					Directory.Delete(info.RootPath, true);
				}
			}
			catch { }
		}

		public static InstallationInfo DuplicateInstallation(InstallationInfo info)
		{
			var destRoot = GetInstallationsPath(info.GameKey);
			Directory.CreateDirectory(destRoot);

			string baseName = info.Name + " - Copy";
			string newName = baseName;
			string newPath = Path.Combine(destRoot, newName);
			int i = 2;
			while (Directory.Exists(newPath))
			{
				newName = $"{baseName} ({i++})";
				newPath = Path.Combine(destRoot, newName);
			}

			DirectoryCopy(info.RootPath, newPath, true);

			return new InstallationInfo
			{
				GameKey = info.GameKey,
				Name = newName,
				Version = info.Version,
				Timestamp = null,
				RootPath = newPath,
				IconName = info.IconName
			};
		}

		public static InstallationInfo RenameInstallation(InstallationInfo info, string newName)
		{
			var root = GetInstallationsPath(info.GameKey);
			Directory.CreateDirectory(root);
			string finalName = newName;
			string destPath = Path.Combine(root, finalName);
			int i = 1;
			while (Directory.Exists(destPath))
			{
				finalName = $"{newName} ({i++})";
				destPath = Path.Combine(root, finalName);
			}

			if (!string.Equals(info.RootPath, destPath, StringComparison.OrdinalIgnoreCase))
			{
				Directory.Move(info.RootPath, destPath);
			}

			return new InstallationInfo
			{
				GameKey = info.GameKey,
				Name = finalName,
				Version = info.Version,
				Timestamp = info.Timestamp,
				RootPath = destPath,
				IconName = info.IconName
			};
		}

		public static void UpdateInstallationIcon(InstallationInfo info, string? iconName)
		{
			var path = Path.Combine(info.RootPath, "installation-info.json");
			var doc = ReadInfoFile(path) ?? new InstallationInfoFile();
			doc.icon = iconName;
			WriteInfoFile(path, doc);
		}

		public static void UpdateInstallationVersion(InstallationInfo info, string version)
		{
			var gameDir = Path.Combine(info.RootPath, "Game");
			try
			{
				if (Directory.Exists(gameDir))
				{
					Directory.Delete(gameDir, true);
				}
				Directory.CreateDirectory(gameDir);
			}
			catch { }

			string versionSource = Path.Combine(GetVersionsPath(info.GameKey), version);
			if (string.Equals(version, "Steam Version", StringComparison.OrdinalIgnoreCase))
			{
				var appId = GetAppId(info.GameKey);
				var steamDir = SteamLocator.FindGamePath(appId);
				if (!string.IsNullOrWhiteSpace(steamDir) && Directory.Exists(steamDir))
				{
					versionSource = steamDir;
				}
			}
			else
			{
				var parts = version.Split('|');
				var manifest = parts[0];
				var branch = parts.Length > 1 ? parts[1] : "public";
				if (manifest.All(char.IsDigit))
				{
					versionSource = EnsureVersionByManifest(info.GameKey, manifest, branch);
				}
			}

			try
			{
				if (Directory.Exists(versionSource))
				{
					DirectoryCopy(versionSource, gameDir, true);
				}
				else
				{
					DownloadGameVersion(info, version); // placeholder
				}
			}
			catch { }

			var path2 = Path.Combine(info.RootPath, "installation-info.json");
			var doc2 = ReadInfoFile(path2) ?? new InstallationInfoFile();
			doc2.version = version;
			WriteInfoFile(path2, doc2);
		}

		public static void DownloadGameVersion(InstallationInfo info, string version)
		{
			// Placeholder
		}

		public static void MarkInstallationLaunched(InstallationInfo info)
		{
			try
			{
				var path = Path.Combine(info.RootPath, "installation-info.json");
				var doc = ReadInfoFile(path) ?? new InstallationInfoFile();
				var now = DateTime.UtcNow;
				doc.timestamp = now.ToString("o");
				WriteInfoFile(path, doc);
				info.Timestamp = now;
			}
			catch { }
		}

		private static InstallationInfoFile? ReadInfoFile(string path)
		{
			try
			{
				if (!File.Exists(path)) return null;
				var json = File.ReadAllText(path);
				return JsonSerializer.Deserialize<InstallationInfoFile>(json);
			}
			catch { return null; }
		}

		private static void WriteInfoFile(string path, InstallationInfoFile doc)
		{
			try
			{
				var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(path, json);
			}
			catch { }
		}

		private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			var dir = new DirectoryInfo(sourceDirName);
			if (!dir.Exists) return;

			Directory.CreateDirectory(destDirName);

			foreach (var file in dir.GetFiles())
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			if (copySubDirs)
			{
				foreach (var subdir in dir.GetDirectories())
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, true);
				}
			}
		}

		private class SteamInfoFile
		{
			public string? timestamp { get; set; }
		}
	}
}

public class InstallationInfo
{
	public string GameKey { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Version { get; set; } = string.Empty;
	public DateTime? Timestamp { get; set; }
	public string RootPath { get; set; } = string.Empty;
	public string? IconName { get; set; }

	public string GamePath => Path.Combine(RootPath, "Game");
	public string DataPath => Path.Combine(RootPath, "Data");
}

internal class InstallationInfoFile
{
	public string? version { get; set; }
	public string? timestamp { get; set; }
	public string? icon { get; set; }
}
