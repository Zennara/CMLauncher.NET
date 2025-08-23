using System;
using System.Collections.Generic;
using System.IO;
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
                    RootPath = dir
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
    }

    public class InstallationInfo
    {
        public string GameKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }
        public string RootPath { get; set; } = string.Empty;

        public string GamePath => Path.Combine(RootPath, "Game");
        public string DataPath => Path.Combine(RootPath, "Data");
    }

    internal class InstallationInfoFile
    {
        public string? version { get; set; }
        public string? timestamp { get; set; }
    }
}
