using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeworkViewer
{
    public static class ManagementHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string RemoteCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Homework", "Remote");

        public static async Task<bool> RefreshAsync(AppConfig config)
        {
            if (!config.MgmtEnabled || string.IsNullOrEmpty(config.MgmtManifestUrl))
                return false;

            try
            {
                string manifestJson = await _httpClient.GetStringAsync(config.MgmtManifestUrl);
                var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson);
                if (manifest == null) return false;

                int currentManifestVer = config.MgmtVersions.GetValueOrDefault("manifest", 0);
                if (manifest.Version <= currentManifestVer)
                    return false;

                config.MgmtVersions["manifest"] = manifest.Version;
                config.OrganizationName = manifest.OrganizationName ?? "";

                if (manifest.SubjectsSource != null && manifest.SubjectsSource.Version > config.MgmtVersions.GetValueOrDefault("subjects", 0))
                {
                    await DownloadAndApplySubjectsAsync(manifest.SubjectsSource.Value, config);
                    config.MgmtVersions["subjects"] = manifest.SubjectsSource.Version;
                }

                if (manifest.HomeworkDataTemplate != null)
                {
                    int currentHomeworkVer = config.MgmtVersions.GetValueOrDefault("homework", 0);
                    if (manifest.HomeworkDataTemplate.Version > currentHomeworkVer)
                    {
                        await DownloadHomeworkDataAsync(manifest.HomeworkDataTemplate.Value, config);
                        config.MgmtVersions["homework"] = manifest.HomeworkDataTemplate.Version;
                    }
                }

                if (manifest.PolicySource != null && manifest.PolicySource.Version > config.MgmtVersions.GetValueOrDefault("policy", 0))
                {
                    await DownloadAndApplyPolicyAsync(manifest.PolicySource.Value, config);
                    config.MgmtVersions["policy"] = manifest.PolicySource.Version;
                }

                config.Save();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"集控刷新失败: {ex.Message}");
                return false;
            }
        }

        private static async Task DownloadAndApplySubjectsAsync(string url, AppConfig config)
        {
            try
            {
                string json = await _httpClient.GetStringAsync(url);
                var subjects = JsonSerializer.Deserialize<List<string>>(json);
            }
            catch { }
        }

        private static async Task DownloadHomeworkDataAsync(string urlTemplate, AppConfig config)
        {
            for (int i = 0; i <= 7; i++)
            {
                DateTime date = DateTime.Today.AddDays(-i);
                string url = urlTemplate.Replace("{date}", date.ToString("yyyy/MM/dd"));
                string localPath = GetHomeworkCachePath(date);

                try
                {
                    string json = await _httpClient.GetStringAsync(url);
                    string dir = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllText(localPath, json);
                }
                catch (HttpRequestException) { }
            }
        }

        private static async Task DownloadAndApplyPolicyAsync(string url, AppConfig config)
        {
            try
            {
                string json = await _httpClient.GetStringAsync(url);
                var policy = JsonSerializer.Deserialize<Policy>(json);
                if (policy != null)
                {
                    config.MgmtForceRemote = policy.ForceUseRemote;
                }
            }
            catch { }
        }

        public static string GetHomeworkCachePath(DateTime date)
        {
            return Path.Combine(RemoteCacheDir, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd") + ".json");
        }

        public static HomeworkData? LoadHomeworkDataFromRemote(DateTime date)
        {
            string path = GetHomeworkCachePath(date);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<HomeworkData>(json) ?? new HomeworkData();
                }
                catch { }
            }
            return null;
        }

        public static bool CanEdit(AppConfig config)
        {
            if (config.MgmtEnabled && config.MgmtForceRemote)
                return false;
            return true;
        }

        public static async void CheckForUpdatesAsync(AppConfig config, Action<bool>? onComplete = null)
        {
            bool updated = await RefreshAsync(config);
            onComplete?.Invoke(updated);
        }
    }

    public class Manifest
    {
        public int ServerKind { get; set; } = 0;
        public string OrganizationName { get; set; } = "";
        public int Version { get; set; } = 1;
        public ReVersionString? SubjectsSource { get; set; }
        public ReVersionString? HomeworkDataTemplate { get; set; }
        public ReVersionString? PolicySource { get; set; }
    }

    public class ReVersionString
    {
        public string Value { get; set; } = "";
        public int Version { get; set; }
    }

    public class Policy
    {
        public bool DisableEditing { get; set; } = false;
        public bool ForceUseRemote { get; set; } = false;
        public bool AllowHistory { get; set; } = true;
    }
}