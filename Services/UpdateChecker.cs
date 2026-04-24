using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SystemMonitorApp.Services
{
    /// <summary>
    /// Non-blocking check against GitHub Releases API to detect newer versions.
    /// No personal data sent — just a HTTP GET with a User-Agent.
    /// Failures are silent (offline, no GitHub, rate-limited) — we never block app startup.
    /// </summary>
    public static class UpdateChecker
    {
        private const string ReleasesUrl = "https://api.github.com/repos/screamm/SystemFlow_Pro/releases/latest";
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

        public sealed record UpdateInfo(string LatestVersion, string ReleaseUrl, string ReleaseNotes);

        public static async Task<UpdateInfo?> CheckAsync()
        {
            try
            {
                using var http = new HttpClient { Timeout = _timeout };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("SystemFlow-Pro");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                var json = await http.GetStringAsync(ReleasesUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);
                if (release == null || string.IsNullOrEmpty(release.TagName)) return null;

                var current = Assembly.GetExecutingAssembly().GetName().Version;
                if (current == null) return null;

                string tag = release.TagName.TrimStart('v');
                // Strip prerelease suffix (e.g., "1.1.0-beta.1" → "1.1.0") for comparison.
                int dash = tag.IndexOf('-');
                string core = dash >= 0 ? tag[..dash] : tag;

                if (!Version.TryParse(core, out var latest)) return null;
                if (latest <= current) return null;

                Logger.Info($"Update available: current={current} latest={release.TagName}");
                return new UpdateInfo(release.TagName, release.HtmlUrl ?? "", release.Body ?? "");
            }
            catch (Exception ex)
            {
                Logger.Info($"Update check failed (silent): {ex.Message}");
                return null;
            }
        }

        private sealed class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = "";

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonPropertyName("body")]
            public string? Body { get; set; }
        }
    }
}
