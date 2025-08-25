using System;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
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

        private static string? _latestPrettyVersion; // includes prerelease tag like -alpha
        private static string? _releaseHtmlUrl;

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

        private static string WriteLocalChangelog(string latestPretty, string installedPretty, string? releaseUrl)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset='utf-8'><style>body{font-family:'Segoe UI',Tahoma,Arial,sans-serif;background:#1e1e1e;color:#ddd;margin:10px} a{color:#4ea3ff}</style></head><body>");
            sb.Append($"<div><strong>Latest:</strong> v{System.Net.WebUtility.HtmlEncode(latestPretty)}</div>");
            sb.Append($"<div><strong>Installed:</strong> v{System.Net.WebUtility.HtmlEncode(installedPretty)}</div>");
            if (!string.IsNullOrWhiteSpace(releaseUrl))
            {
                sb.Append($"<div style='margin-top:8px'><a href='{System.Net.WebUtility.HtmlEncode(releaseUrl)}'>View release on GitHub</a></div>");
            }
            sb.Append("</body></html>");
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CMLauncher_UpdateNotes.html");
            System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
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
                var installedPretty = GetInformationalVersionNormalized();
                var installedNumeric = StripPrerelease(installedPretty);
                try { AutoUpdater.InstalledVersion = new Version(installedNumeric); } catch { /* ignore */ }

                AutoUpdater.ParseUpdateInfoEvent += args =>
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(args.RemoteData);
                        var root = doc.RootElement;
                        var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? string.Empty : string.Empty;
                        _releaseHtmlUrl = root.TryGetProperty("html_url", out var htmlEl) ? htmlEl.GetString() : null;

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

                        var normalized = NormalizeVersion(tag); // keep -alpha for display
                        _latestPrettyVersion = normalized;
                        var normalizedNumeric = StripPrerelease(normalized); // numeric for comparison

                        var localChangelog = WriteLocalChangelog(_latestPrettyVersion, installedPretty, _releaseHtmlUrl);

                        args.UpdateInfo = new UpdateInfoEventArgs
                        {
                            CurrentVersion = normalizedNumeric,
                            ChangelogURL = localChangelog,
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

                        bool isAvailable = updateInfo.IsUpdateAvailable;
                        // Force update dialog if latest is stable and installed is prerelease (numeric equal)
                        if (!isAvailable && !string.IsNullOrWhiteSpace(_latestPrettyVersion))
                        {
                            bool latestIsStable = _latestPrettyVersion.IndexOf('-') < 0;
                            bool installedIsPrerelease = installedPretty.IndexOf('-') >= 0;
                            if (latestIsStable && installedIsPrerelease)
                            {
                                isAvailable = true;
                            }
                        }

                        if (!isAvailable)
                        {
                            if (!silentIfUpToDate)
                                MessageBox.Show(owner, $"You are up to date. (v{installedPretty})", "CastleMiner Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        // Directly show AutoUpdater dialog (no extra popup)
                        AutoUpdater.ShowUpdateForm(updateInfo);
                    });
                };

                AutoUpdater.Start(LatestReleaseApi);
            });
        }
    }
}
