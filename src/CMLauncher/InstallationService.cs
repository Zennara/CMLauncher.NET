using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;

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

            // Steam Version: prefer copying from actual Steam install path
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

            // Copy version files into Game if a matching source exists
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
            var json = JsonSerializer.Serialize(new InstallationInfoFile
            {
                version = version,
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

            // Prefer Steam install for Steam Version
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

            // Update info file
            var path = Path.Combine(info.RootPath, "installation-info.json");
            var doc = ReadInfoFile(path) ?? new InstallationInfoFile();
            doc.version = version;
            WriteInfoFile(path, doc);
        }

        public static void DownloadGameVersion(InstallationInfo info, string version)
        {
            // Placeholder: intentionally do nothing for now
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

        public static string? GetSteamInstallPath(string gameKey)
        {
            // Prefer user-saved path, then detected
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
                // Prefer FileVersion; fallback to ProductVersion
                return !string.IsNullOrWhiteSpace(info.FileVersion) ? info.FileVersion : info.ProductVersion;
            }
            catch { return null; }
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
}
