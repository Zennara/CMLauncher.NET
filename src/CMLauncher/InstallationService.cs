using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows; // added for login prompt on auth failure during operations
using System.Windows.Threading;

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

		public static void UpdateInstallationVersion(InstallationInfo info, string selectedKey)
		{
			// Clear Game folder
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

			var manifestId = GetVersionFolderName(selectedKey);

			// Prefer Steam install for Steam Version
			string versionSource = Path.Combine(GetVersionsPath(info.GameKey), manifestId);
			if (string.Equals(selectedKey, "Steam Version", StringComparison.OrdinalIgnoreCase))
			{
				var appId = GetAppId(info.GameKey);
				var steamDir = SteamLocator.FindGamePath(appId);
				if (!string.IsNullOrWhiteSpace(steamDir) && Directory.Exists(steamDir))
				{
					versionSource = steamDir;
				}
			}

			try
			{
				if (Directory.Exists(versionSource))
				{
					// If version files already exist, copy them into Game (preserve cache)
					CopyDirectoryContents(versionSource, gameDir);
				}
				else
				{
					DownloadGameVersion(info, selectedKey);
				}
			}
			catch { }

			// Update info file (manifest + friendly version)
			var path = Path.Combine(info.RootPath, "installation-info.json");
			var doc = ReadInfoFile(path) ?? new InstallationInfoFile();
			doc.manifest = manifestId;
			doc.version = ResolveFriendlyVersionFromMapping(info.GameKey, manifestId)
				?? GetDisplayVersionForInstallation(new InstallationInfo { GameKey = info.GameKey, RootPath = info.RootPath, Name = info.Name, Version = manifestId })
				?? manifestId;
			WriteInfoFile(path, doc);
		}

		public static void UpdateInstallationIcon(InstallationInfo info, string? iconName)
		{
			var path = Path.Combine(info.RootPath, "installation-info.json");
			var doc = ReadInfoFile(path) ?? new InstallationInfoFile();
			doc.icon = iconName;
			WriteInfoFile(path, doc);
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

		private static string? GetSteamMetaPath(string gameKey)
		{
			var dir = GetSteamInstallPath(gameKey);
			if (string.IsNullOrWhiteSpace(dir)) return null;
			return Path.Combine(dir, "cm_launcher_installation-info.json");
		}

		public static string? GetSteamInstallPath(string gameKey)
		{
			// Prefer user-saved path, then detected
			return LauncherSettings.Current.GetSteamPathForGame(gameKey) ?? SteamLocator.FindGamePath(GetAppId(gameKey));
		}

		public static DateTime? GetSteamLastPlayed(string gameKey)
		{
			try
			{
				var meta = GetSteamMetaPath(gameKey);
				if (meta == null || !File.Exists(meta)) return null;
				var json = File.ReadAllText(meta);
				var doc = JsonSerializer.Deserialize<InstallationService.SteamInfoFile>(json);
				if (doc != null && !string.IsNullOrWhiteSpace(doc.timestamp) && DateTime.TryParse(doc.timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
				{
					return dt;
				}
			}
			catch { }
			return null;
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

		public static InstallationInfo CreateInstallation(string gameKey, string name, string selectedKey, string? iconName = null)
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

			// Choose a random icon if none provided
			if (string.IsNullOrWhiteSpace(iconName))
			{
				var icons = LoadAvailableIcons();
				if (icons.Count > 0)
				{
					var rnd = new Random();
					iconName = icons[rnd.Next(icons.Count)];
				}
			}

			var manifestId = GetVersionFolderName(selectedKey);

			// Steam Version: prefer copying from actual Steam install path
			var versionSource = Path.Combine(GetVersionsPath(gameKey), manifestId);
			if (string.Equals(selectedKey, "Steam Version", StringComparison.OrdinalIgnoreCase))
			{
				var appId = GetAppId(gameKey);
				var steamDir = SteamLocator.FindGamePath(appId);
				if (!string.IsNullOrWhiteSpace(steamDir) && Directory.Exists(steamDir))
				{
					versionSource = steamDir;
				}
			}

			// Copy version files into Game if a matching source exists (preserve cache)
			try
			{
				if (Directory.Exists(versionSource))
				{
					CopyDirectoryContents(versionSource, gameDir);
				}
			}
			catch { }

			var info = new InstallationInfo
			{
				GameKey = gameKey,
				Name = finalName,
				Version = manifestId, // model stores manifest id for logic
				Timestamp = null,
				RootPath = candidatePath,
				IconName = iconName
			};

			var infoFile = Path.Combine(candidatePath, "installation-info.json");
			var friendly = ResolveFriendlyVersionFromMapping(gameKey, manifestId)
				?? GetDisplayVersionForInstallation(info)
				?? manifestId;
			var json = JsonSerializer.Serialize(new InstallationInfoFile
			{
				version = friendly,
				manifest = manifestId,
				timestamp = null,
				icon = iconName
			}, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(infoFile, json);

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
			catch
			{
				// ignore IO errors for now
			}
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
				// Prefer FileVersion; fallback to ProductVersion
				return !string.IsNullOrWhiteSpace(info.FileVersion) ? info.FileVersion : info.ProductVersion;
			}
			catch { return null; }
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

		private static void WriteInfoFile(string path, InstallationInfoFile doc)
		{
			try
			{
				var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(path, json);
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

		public static void DownloadGameVersion(InstallationInfo info, string selectedKey)
		{
			try
			{
				string manifest = GetVersionFolderName(selectedKey);
				string branch = "public";
				if (selectedKey.Contains('|'))
				{
					var parts = selectedKey.Split('|');
					manifest = parts[0];
					if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) branch = parts[1];
				}

				if (!manifest.All(char.IsDigit)) return;

				Debug.WriteLine($"Ensuring version manifest {manifest} (branch {branch}) for {info.GameKey}");
				var ensured = EnsureVersionByManifest(info.GameKey, manifest, branch);
				var gameDir = Path.Combine(info.RootPath, "Game");
				if (Directory.Exists(ensured))
				{
					Debug.WriteLine($"Moving files from {ensured} to {gameDir}");
					MoveDirectoryContents(ensured, gameDir);
				}

				// Persist manifest + friendly version
				var path = Path.Combine(info.RootPath, "installation-info.json");
				var friendly = ResolveFriendlyVersionFromMapping(info.GameKey, manifest)
					?? GetDisplayVersionForInstallation(info)
					?? manifest;
				var doc = ReadInfoFile(path) ?? new InstallationInfoFile();
				doc.manifest = manifest;
				doc.version = friendly;
				WriteInfoFile(path, doc);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"DownloadGameVersion error: {ex.Message}");
			}
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
				string manifest = string.Empty;
				string? friendly = null;
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
							manifest = doc.manifest ?? string.Empty;
							friendly = doc.version;
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

				// If friendly missing, compute it from files lazily
				if (string.IsNullOrWhiteSpace(friendly))
				{
					try
					{
						var tmpInfo = new InstallationInfo { GameKey = gameKey, RootPath = dir, Name = name, Version = manifest };
						friendly = GetDisplayVersionForInstallation(tmpInfo);
					}
					catch { }
				}

				list.Add(new InstallationInfo
				{
					GameKey = gameKey,
					Name = name,
					Version = manifest, // store manifest key in model for logic comparisons
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
				var ddExe = GetDepotDownloaderExePath();
				if (string.IsNullOrWhiteSpace(ddExe) || !File.Exists(ddExe)) return target;

				var appId = GetAppId(gameKey);
				var depotId = gameKey == CMZKey ? "253431" : "675211";
				Directory.CreateDirectory(target);
				var branchArg = string.Equals(branch, "public", StringComparison.OrdinalIgnoreCase) ? string.Empty : $" -branch {branch}";
				var creds = BuildCredentialArgs();

				DownloadProgressWindow? dlg = null;
				Application.Current?.Dispatcher.Invoke(() =>
				{
					dlg = new DownloadProgressWindow("Downloading version...");
					dlg?.Show();
				});

				bool accessDenied = false;
				var percentRegex = new System.Text.RegularExpressions.Regex("(?<p>\\d{1,3}(?:\\.\\d+)?)(?=%)");
				var launcher = new DepotDownloaderWrapper(ddExe!);
				launcher.OutputReceived += line =>
				{
					try
					{
						Debug.WriteLine(line);
						Application.Current?.Dispatcher.Invoke(() => dlg?.AppendLog(line));
						var lower = line.ToLowerInvariant();
						if (!accessDenied && lower.Contains("access token was rejected") && lower.Contains("accessdenied"))
						{
							accessDenied = true;
						}

						// Friendly status updates (keep window text short)
						if (lower.Contains("pre-allocating "))
						{
							Application.Current?.Dispatcher.Invoke(() => dlg?.UpdateStatus("Pre-allocating files"));
						}
						else if (lower.Contains("validating "))
						{
							Application.Current?.Dispatcher.Invoke(() => dlg?.UpdateStatus("Validating files"));
						}
						else if (lower.StartsWith("processing depot"))
						{
							Application.Current?.Dispatcher.Invoke(() => dlg?.UpdateStatus("Preparing download..."));
						}
						else if (lower.StartsWith("downloading depot"))
						{
							Application.Current?.Dispatcher.Invoke(() => dlg?.UpdateStatus("Downloading files"));
						}

						var m = percentRegex.Match(line);
						if (m.Success && double.TryParse(m.Groups["p"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var p))
						{
							Application.Current?.Dispatcher.Invoke(() => dlg?.UpdateProgress(p, "Downloading files"));
						}
					}
					catch { }
				};

				var args = $"-app {appId} -depot {depotId} -manifest {manifestId}{branchArg}{creds} -remember-password -dir \"{target}\"";
				var runTask = launcher.RunAsync(args);
				// Keep UI responsive while waiting
				WaitWithUiPump(runTask);

				Application.Current?.Dispatcher.Invoke(() => { try { dlg?.Close(); } catch { } });

				if (accessDenied)
				{
					HandleAuthFailureDuringOperation();
				}
			}
			catch { }

			return target;
		}

		// Pump UI messages while waiting so the progress window updates
		private static void WaitWithUiPump(System.Threading.Tasks.Task task)
		{
			try
			{
				var dispatcher = Application.Current?.Dispatcher;
				if (dispatcher != null && dispatcher.CheckAccess())
				{
					while (!task.IsCompleted)
					{
						// Process pending messages
						dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
						Thread.Sleep(15);
					}
					// Observe exceptions if any
					task.GetAwaiter().GetResult();
				}
				else
				{
					task.Wait();
				}
			}
			catch { }
		}

		private static string GetVersionFolderName(string version)
		{
			if (string.IsNullOrWhiteSpace(version)) return version;
			var pipe = version.IndexOf('|');
			if (pipe > 0)
			{
				return version.Substring(0, pipe).Trim();
			}
			return version.Trim();
		}

		private static void MoveDirectoryContents(string sourceDir, string destDir)
		{
			try
			{
				Directory.CreateDirectory(destDir);
				if (!Directory.Exists(sourceDir)) return;

				// Move files
				foreach (var file in Directory.GetFiles(sourceDir))
				{
					var name = Path.GetFileName(file);
					var destPath = Path.Combine(destDir, name);
					try
					{
						if (File.Exists(destPath)) File.Delete(destPath);
						File.Move(file, destPath);
					}
					catch
					{
						// fallback to copy
						try { File.Copy(file, destPath, overwrite: true); File.Delete(file); } catch { }
					}
				}

				// Move subdirectories (skip DepotDownloader metadata folder)
				foreach (var dir in Directory.GetDirectories(sourceDir))
				{
					var name = Path.GetFileName(dir);
					if (string.Equals(name, ".DepotDownloader", StringComparison.OrdinalIgnoreCase))
						continue;
					var destPath = Path.Combine(destDir, name);
					try
					{
						if (Directory.Exists(destPath)) Directory.Delete(destPath, true);
						Directory.Move(dir, destPath);
					}
					catch
					{
						// fallback to copy
						try { DirectoryCopy(dir, destPath, true); Directory.Delete(dir, true); } catch { }
					}
				}
			}
			catch { }
		}

		private static void CopyDirectoryContents(string sourceDir, string destDir)
		{
			try
			{
				Directory.CreateDirectory(destDir);
				if (!Directory.Exists(sourceDir)) return;

				// Copy files
				foreach (var file in Directory.GetFiles(sourceDir))
				{
					var name = Path.GetFileName(file);
					var destPath = Path.Combine(destDir, name);
					try { File.Copy(file, destPath, overwrite: true); } catch { }
				}

				// Copy subdirectories (skip DepotDownloader metadata folder)
				foreach (var dir in Directory.GetDirectories(sourceDir))
				{
					var name = Path.GetFileName(dir);
					if (string.Equals(name, ".DepotDownloader", StringComparison.OrdinalIgnoreCase))
						continue;
					var destPath = Path.Combine(destDir, name);
					try { DirectoryCopy(dir, destPath, true); } catch { }
				}
			}
			catch { }
		}

		public static string? TryGetExeVersionFromFolder(string gameKey, string folder)
		{
			try
			{
				var exeName = GetExeName(gameKey);
				var path = Path.Combine(folder, exeName);
				if (!File.Exists(path))
				{
					// fallback: search recursively (may be slower but only used for listing)
					var matches = Directory.GetFiles(folder, exeName, SearchOption.AllDirectories);
					path = matches.FirstOrDefault();
					if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
				}
				var info = FileVersionInfo.GetVersionInfo(path);
				return !string.IsNullOrWhiteSpace(info.FileVersion) ? info.FileVersion : info.ProductVersion;
			}
			catch { return null; }
		}

		public static List<(string key, string display)> LoadAvailableVersionsDetailed(string gameKey)
		{
			var list = new List<(string key, string display)>();
			var path = GetVersionsPath(gameKey);
			if (!Directory.Exists(path)) return list;
			foreach (var dir in Directory.GetDirectories(path))
			{
				var key = Path.GetFileName(dir);
				var display = TryGetExeVersionFromFolder(gameKey, dir) ?? key;
				list.Add((key, display));
			}
			list.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.display, b.display));
			return list;
		}

		public static string ExtractDisplayVersionFromExeVersion(string? exeVersion)
		{
			if (string.IsNullOrWhiteSpace(exeVersion)) return "Unknown";
			// Prefer ProductVersion if it's richer; FileVersion may drop suffixes
			return exeVersion.Trim();
		}

		public static string GetDisplayVersionForInstallation(InstallationInfo info)
		{
			try
			{
				// Steam pseudo-installation: read directly from Steam exe
				if (string.IsNullOrWhiteSpace(info.RootPath) || string.Equals(info.Name, "Steam Installation", StringComparison.OrdinalIgnoreCase))
				{
					return ExtractDisplayVersionFromExeVersion(GetSteamExeVersion(info.GameKey)) ?? "Steam Version";
				}

				// Prefer stored friendly version in installation-info.json
				var infoPath = Path.Combine(info.RootPath, "installation-info.json");
				if (File.Exists(infoPath))
				{
					try
					{
						var json = File.ReadAllText(infoPath);
						var doc = JsonSerializer.Deserialize<InstallationInfoFile>(json);
						if (!string.IsNullOrWhiteSpace(doc?.version))
							return doc!.version!;
					}
					catch { }
				}

				// Fallback to exe version from Game directory
				var gameDir = Path.Combine(info.RootPath ?? string.Empty, "Game");
				var exeVer = TryGetExeVersionFromFolder(info.GameKey, gameDir);
				if (!string.IsNullOrWhiteSpace(exeVer)) return ExtractDisplayVersionFromExeVersion(exeVer);
			}
			catch { }
			return "Unknown";
		}

		public static string? ResolveFriendlyVersionFromMapping(string gameKey, string manifestId)
		{
			try
			{
				if (string.Equals(gameKey, CMZKey, StringComparison.OrdinalIgnoreCase) && manifestId.All(char.IsDigit))
				{
					var list = ManifestService.FetchCmzManifestsAsync().GetAwaiter().GetResult();
					var match = list.FirstOrDefault(x => string.Equals(x.ManifestId, manifestId, StringComparison.OrdinalIgnoreCase));
					if (!string.IsNullOrWhiteSpace(match?.Version)) return match!.Version!;
				}
			}
			catch { }
			return null;
		}
	}
}
