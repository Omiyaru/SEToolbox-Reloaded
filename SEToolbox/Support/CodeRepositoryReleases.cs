using Octokit;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SEToolbox.Support
{
    /// <summary>
    /// Extracts the GitHub website information to determine the version of the current release.
    /// </summary>
    public class CodeRepositoryReleases
    {
        public static ApplicationRelease CheckForUpdates(Version currentVersion, bool dontIgnore = false)
        {
            if (!dontIgnore)
            {
                if (GlobalSettings.Default.AlwaysCheckForUpdates.HasValue && !GlobalSettings.Default.AlwaysCheckForUpdates.Value)
                {
                    return null;
                }

#if DEBUG
                // Skip the load check, as it may take a few seconds during development.
                if (Debugger.IsAttached)
                {
                    return null;
                }
#endif
            }

            // Accessing GitHub API directly for updates.
            GitHubClient client = new(new ProductHeaderValue("SEToolbox-Updater"));

            Release latest = null;

            try
            {
                latest = client.Repository.Release.GetLatest("mmusu3", "SEToolbox").Result;//omiyaru
            }
            catch (Exception ex)
            {
                // Network connection error.
                if (ex?.InnerException is HttpRequestException ||
                    ex?.InnerException?.InnerException is WebException)
                {
                    Log.WriteLine($"An error occurred while checking for updates: {ex.Message}");
                    return null;
                }

                throw;
            }

            ApplicationRelease item = new()
            {
                Name = latest.Name,
                Link = latest.HtmlUrl,
                Version = GetVersion(latest.TagName),
                Notes = latest.Body
            };

            Version.TryParse(GlobalSettings.Default.IgnoreUpdateVersion, out Version ignoreVersion);

            return item.Version > currentVersion && item.Version != ignoreVersion || dontIgnore ? item : null;
        }


        private static Version GetVersion(string version)
        {
            var matchString = @"v?(?<v1>\d+)\.(?<v2>\d+)\.(?<v3>\d+)\sRelease\s(?<v4>\d+)"?? @"v?(?<v1>\d+)\.(?<v2>\d+)\.(?<v3>\d+)\sRelease\s(?<v4>\d+)";
            Match match = Regex.Match(version, matchString);
            string[] matchGroups = ["v1", "v2", "v3", "v4"];
            int i = 0;
            string matchGroup = matchGroups[i] + ".";
            if (match.Success)
            {
                var versionParts = match.Groups.Cast<string>().Where(g => g != null).Select(g => match.Groups[g].Value).ToArray();
                return new Version(string.Join(".", versionParts));
            }
            return new Version(0, 0, 0, 0);
        }
    }

    public class ApplicationRelease
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public Version Version { get; set; }
        public string Notes { get; set; }
    }
}
