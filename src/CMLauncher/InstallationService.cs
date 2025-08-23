using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CMLauncher
{
    public static class InstallationService
    {
        public const string RootFolderName = ".castleminer";
        public const string CMZKey = "CMZ";
        public const string CMWKey = "CMW";

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
            Directory.CreateDirectory(Path.Combine(candidatePath, "Game"));
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

            var info = new InstallationInfo
            {
                GameKey = gameKey,
                Name = finalName,
                Version = version,
                Timestamp = null, // last played set elsewhere
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

            return info;
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
