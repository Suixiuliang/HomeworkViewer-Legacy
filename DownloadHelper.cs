#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeworkViewer
{
    public class DownloadProgressInfo
    {
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        public int Percentage { get; set; }
        public double SpeedKBps { get; set; }
        public TimeSpan TimeRemaining { get; set; }
    }

    public class ReleaseAsset
    {
        public string Name { get; set; }
        public string DownloadUrl { get; set; }
        public long Size { get; set; }
        public string ContentType { get; set; }
    }

    public class DownloadHelper
    {
        private readonly HttpClient _httpClient;
        private readonly MirrorManager _mirrorManager;
        private readonly string _tempPath;
        public bool UseMirror { get; set; } = true;  // 是否启用镜像加速

        public DownloadHelper()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,           // 自动跟随重定向
                MaxAutomaticRedirections = 5
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HomeworkViewer-Updater");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/octet-stream, */*");

            _mirrorManager = new MirrorManager();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _tempPath = Path.Combine(appData, "HomeworkViewerUpgrader");
            if (!Directory.Exists(_tempPath))
            {
                Directory.CreateDirectory(_tempPath);
            }
        }

        /// <summary>
        /// 修正常见的错误下载链接（将 /tag/ 替换为 /download/）
        /// </summary>
        private string FixDownloadUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            // 例如: https://github.com/owner/repo/releases/tag/v1.0/file.exe
            // 应该改为: https://github.com/owner/repo/releases/download/v1.0/file.exe
            if (url.Contains("/releases/tag/"))
            {
                url = url.Replace("/releases/tag/", "/releases/download/");
                // 确保末尾有文件名（如果没有，尝试从 URL 最后一段提取）
                if (url.EndsWith("/"))
                {
                    // 不合理，保持原样
                }
            }
            return url;
        }

        public async Task<List<ReleaseAsset>> GetLatestReleaseAssetsAsync(string owner, string repo)
        {
            string apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            var result = new List<ReleaseAsset>();

            try
            {
                string json = await _httpClient.GetStringAsync(apiUrl);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("assets", out JsonElement assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var releaseAsset = new ReleaseAsset
                        {
                            Name = asset.GetProperty("name").GetString(),
                            DownloadUrl = asset.GetProperty("browser_download_url").GetString(),
                            Size = asset.GetProperty("size").GetInt64(),
                            ContentType = asset.GetProperty("content_type").GetString()
                        };
                        result.Add(releaseAsset);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取Release信息失败: {ex.Message}");
            }
            return result;
        }

        public ReleaseAsset FindBestMatchAsset(List<ReleaseAsset> assets)
        {
            if (assets == null || assets.Count == 0) return null;

            var candidates = assets.Where(a =>
            {
                string name = a.Name.ToLower();
                return (name.Contains("windows") || name.Contains("win")) &&
                       (name.EndsWith(".exe") || name.EndsWith(".msi") || name.EndsWith(".zip"));
            }).ToList();

            if (candidates.Count == 0)
                candidates = assets.Where(a => a.Name.ToLower().EndsWith(".exe") || a.Name.ToLower().EndsWith(".msi")).ToList();

            return candidates.OrderByDescending(a => a.Name.Length).FirstOrDefault();
        }

        /// <summary>
        /// 下载文件，支持镜像、HEAD预检、大小检查、哈希校验
        /// </summary>
        public async Task<string> DownloadFileAsync(string downloadUrl, string fileName, string expectedHash = null, IProgress<DownloadProgressInfo> progress = null)
        {
            // 修复 URL（如果用户错误地使用了 /tag/ 链接）
            downloadUrl = FixDownloadUrl(downloadUrl);
            string localPath = Path.Combine(_tempPath, fileName);
            if (File.Exists(localPath)) try { File.Delete(localPath); } catch { }

            // 构建尝试的 URL 列表（先镜像，后原始）
            List<string> urlsToTry = new List<string>();
            if (UseMirror)
            {
                try
                {
                    string mirroredUrl = await _mirrorManager.GetMirroredUrlAsync(downloadUrl);
                    urlsToTry.Add(mirroredUrl);
                }
                catch { /* 忽略镜像获取失败 */ }
            }
            urlsToTry.Add(downloadUrl); // 最后原始 URL

            Exception lastException = null;

            for (int attempt = 0; attempt < urlsToTry.Count; attempt++)
            {
                string currentUrl = urlsToTry[attempt];
                try
                {
                    // 1. HEAD 请求预检（验证 URL 是否有效，内容类型是否合理）
                    using (var headRequest = new HttpRequestMessage(HttpMethod.Head, currentUrl))
                    using (var headResponse = await _httpClient.SendAsync(headRequest))
                    {
                        if (!headResponse.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException($"HEAD 请求失败: {(int)headResponse.StatusCode} {headResponse.ReasonPhrase}");
                        }

                        string contentType = headResponse.Content.Headers.ContentType?.MediaType ?? "";
                        long contentLength = headResponse.Content.Headers.ContentLength ?? -1;

                        // 如果内容类型是 HTML 或文件极小（< 500KB），认为是错误页面或重定向页
                        if (contentType.Contains("html") || (contentLength > 0 && contentLength < 500 * 1024))
                        {
                            throw new Exception($"无效响应: Content-Type={contentType}, 大小={contentLength} 字节，可能并非真实文件");
                        }

                        // 可选：如果是 GitHub 的下载链接，最终重定向后的 Content-Length 应该与 Release 中的 Size 匹配
                    }

                    // 2. 实际下载（支持进度报告）
                    using (var response = await _httpClient.GetAsync(currentUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        long totalBytes = response.Content.Headers.ContentLength ?? -1;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;
                            var lastReportTime = DateTime.Now;
                            long lastReportBytes = 0;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                if (progress != null && totalBytes > 0)
                                {
                                    var now = DateTime.Now;
                                    if ((now - lastReportTime).TotalMilliseconds >= 100)
                                    {
                                        int percentage = (int)((totalRead * 100) / totalBytes);
                                        double speedKBps = (totalRead - lastReportBytes) / (now - lastReportTime).TotalSeconds / 1024.0;
                                        TimeSpan timeRemaining = speedKBps > 0 ? TimeSpan.FromSeconds((totalBytes - totalRead) / (speedKBps * 1024)) : TimeSpan.Zero;
                                        var info = new DownloadProgressInfo
                                        {
                                            BytesReceived = totalRead,
                                            TotalBytes = totalBytes,
                                            Percentage = percentage,
                                            SpeedKBps = speedKBps,
                                            TimeRemaining = timeRemaining
                                        };
                                        progress.Report(info);
                                        lastReportBytes = totalRead;
                                        lastReportTime = now;
                                    }
                                }
                            }
                            if (progress != null && totalBytes > 0)
                            {
                                progress.Report(new DownloadProgressInfo { BytesReceived = totalBytes, TotalBytes = totalBytes, Percentage = 100 });
                            }
                        }
                    }

                    // 3. 下载后检查文件大小（至少 500KB）
                    var fileInfo = new FileInfo(localPath);
                    if (fileInfo.Length < 500 * 1024)
                    {
                        File.Delete(localPath);
                        throw new Exception($"下载的文件过小（{fileInfo.Length}字节），可能为错误页面（如 404、403）");
                    }

                    // 4. 哈希校验
                    if (!string.IsNullOrEmpty(expectedHash))
                    {
                        string actualHash = await Task.Run(() => ComputeSHA256(localPath));
                        // 移除可能的 "sha256:" 前缀
                        if (expectedHash.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                            expectedHash = expectedHash.Substring(7);
                        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(localPath);
                            throw new Exception($"哈希校验失败！\n期望: {expectedHash}\n实际: {actualHash}");
                        }
                    }

                    return localPath; // 成功
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Debug.WriteLine($"下载失败 (尝试 {attempt + 1}/{urlsToTry.Count}, URL={currentUrl}): {ex.Message}");
                    if (attempt == urlsToTry.Count - 1)
                        break; // 最后一次，不再重试
                    await Task.Delay(1000 * (attempt + 1));
                }
            }

            throw lastException ?? new Exception("下载失败，所有 URL 均无效");
        }

        private string ComputeSHA256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public void OpenOrInstallFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".exe" || ext == ".msi")
            {
                Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true, Verb = "runas" });
            }
            else
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
        }

        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_tempPath))
                {
                    foreach (var file in Directory.GetFiles(_tempPath)) try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }

        public string GetTempPath() => _tempPath;
    }
}