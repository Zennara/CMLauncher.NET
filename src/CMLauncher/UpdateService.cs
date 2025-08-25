using System;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;

namespace CMLauncher
{
    internal static class UpdateService
    {
        private const string RepoOwner = "Zennara";
        private const string RepoName = "CMLauncher.NET";
        private static readonly string LatestReleaseApi = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

        private static string GetInformationalVersionNormalized()
        {
            var info = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            return NormalizeVersion(info);
        }

        private static string NormalizeVersion(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "0.0.0";
            v = v.Trim();
            if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase)) v = v.Substring(1);
            var plus = v.IndexOf('+');
            if (plus >= 0) v = v.Substring(0, plus);
            return v;
        }

        private static string StripPrerelease(string v)
        {
            var dash = v.IndexOf('-');
            return dash >= 0 ? v.Substring(0, dash) : v;
        }

        public static async Task CheckAndPromptAsync(Window owner, bool silentIfUpToDate, CancellationToken ct = default)
        {
            await owner.Dispatcher.InvokeAsync(() =>
            {
                AutoUpdater.AppTitle = "CastleMiner Launcher";
                AutoUpdater.HttpUserAgent = new ProductInfoHeaderValue("CMLauncher", GetInformationalVersionNormalized()).ToString();
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.ReportErrors = !silentIfUpToDate;
                AutoUpdater.DownloadPath = System.IO.Path.GetTempPath();
                // InstalledVersion must be numeric Version
                var installedNumeric = StripPrerelease(GetInformationalVersionNormalized());
                try { AutoUpdater.InstalledVersion = new Version(installedNumeric); } catch { /* ignore */ }

                AutoUpdater.ParseUpdateInfoEvent += args =>
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(args.RemoteData);
                        var root = doc.RootElement;
                        var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? string.Empty : string.Empty;
                        var html = root.TryGetProperty("html_url", out var htmlEl) ? htmlEl.GetString() : null;

                        string? zipUrl = null;
                        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var a in assets.EnumerateArray())
                            {
                                var name = a.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                                var url = a.TryGetProperty("browser_download_url", out var urlEl) ? urlEl.GetString() : null;
                                if (!string.IsNullOrEmpty(url) && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                {
                                    zipUrl = url;
                                    break;
                                }
                            }
                        }

                        var normalized = NormalizeVersion(tag);
                        var normalizedNumeric = StripPrerelease(normalized);
                        args.UpdateInfo = new UpdateInfoEventArgs
                        {
                            CurrentVersion = normalizedNumeric,
                            ChangelogURL = html,
                            Mandatory = new Mandatory(),
                            DownloadURL = zipUrl
                        };
                    }
                    catch
                    {
                    }
                };

                AutoUpdater.CheckForUpdateEvent += (updateInfo) =>
                {
                    owner.Dispatcher.Invoke(() =>
                    {
                        if (updateInfo.Error != null)
                        {
                            if (!silentIfUpToDate)
                                MessageBox.Show(owner, "Failed to check for updates. Check your internet connection. Continuing in offline mode.", "CastleMiner Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (!updateInfo.IsUpdateAvailable)
                        {
                            if (!silentIfUpToDate)
                                MessageBox.Show(owner, $"You are up to date. (v{GetInformationalVersionNormalized()})", "CastleMiner Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        AutoUpdater.ShowUpdateForm(updateInfo);
                    });
                };

                AutoUpdater.Start(LatestReleaseApi);
            });
        }
    }
}
