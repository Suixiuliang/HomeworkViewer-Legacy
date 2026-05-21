#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeworkViewer
{
    public class MirrorManager
    {
        // 国内稳定镜像列表（按推荐顺序）
        private readonly List<string> _mirrorSites = new List<string>
        {
            "https://ghproxy.net",
            "https://ghproxy.org",
            "https://ghp.ci",
            "https://mirror.ghproxy.com",
            "https://gitclone.com",
            "https://hub.fgit.ml",
            // 备选（可能较慢）
            "https://ghproxy.homeboyc.cn",
            "https://kkgithub.com",
            "https://bgithub.xyz"
        };

        private List<string> _workingMirrors;
        private DateTime _lastTestTime = DateTime.MinValue;
        private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(10); // 缩短缓存时间
        private readonly HttpClient _httpClient;

        public MirrorManager()
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false }; // 避免重定向干扰检测
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(2); // 超时短一点
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HomeworkViewer-MirrorChecker");
        }

        /// <summary>
        /// 获取可用的镜像列表（不检测 HEAD 可用性，直接返回完整列表，由外层尝试下载）
        /// </summary>
        public async Task<List<string>> GetWorkingMirrorsAsync()
        {
            if (_workingMirrors != null && DateTime.Now - _lastTestTime < _cacheTTL)
                return _workingMirrors;

            // 并发快速检测哪些镜像能连通（仅 TCP 连接测试，不要求 HEAD 成功）
            var testTasks = _mirrorSites.Select(async mirror =>
            {
                bool reachable = await TestMirrorConnectivityAsync(mirror);
                return (mirror, reachable);
            });
            var results = await Task.WhenAll(testTasks);

            _workingMirrors = results.Where(r => r.reachable).Select(r => r.mirror).ToList();
            if (_workingMirrors.Count == 0)
                _workingMirrors = _mirrorSites; // 一个都没通？那就全用，死马当活马医
            _lastTestTime = DateTime.Now;
            return _workingMirrors;
        }

        /// <summary>
        /// 仅测试镜像是否可达（不关心内容是否正确）
        /// </summary>
        private async Task<bool> TestMirrorConnectivityAsync(string mirror)
        {
            try
            {
                // 只测根路径，HEAD 请求很多镜像不支持，改用 GET 但只读一下响应头
                var request = new HttpRequestMessage(HttpMethod.Get, mirror);
                // 设置 Range 头，只请求第一个字节，减少数据传输
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.PartialContent;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取最快的镜像（直接返回工作镜像列表的第一个）
        /// </summary>
        public async Task<string> GetFastestMirrorAsync()
        {
            var mirrors = await GetWorkingMirrorsAsync();
            return mirrors.FirstOrDefault() ?? "https://github.com";
        }

        /// <summary>
        /// 将原始 GitHub URL 转换为镜像 URL
        /// </summary>
        public async Task<string> GetMirroredUrlAsync(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl) || !originalUrl.Contains("github.com"))
                return originalUrl;

            string fastestMirror = await GetFastestMirrorAsync();
            if (fastestMirror == "https://github.com")
                return originalUrl;

            // 不同镜像的 URL 拼接规则可能不同
            if (fastestMirror.Contains("ghproxy") || fastestMirror.Contains("gh-proxy") || fastestMirror.Contains("ghp.ci"))
            {
                // 这类代理需要将完整 URL 作为路径参数
                return $"{fastestMirror}/{originalUrl}";
            }
            else
            {
                // 这类镜像直接替换域名
                return originalUrl.Replace("https://github.com", fastestMirror);
            }
        }

        public void ClearCache()
        {
            _workingMirrors = null;
            _lastTestTime = DateTime.MinValue;
        }
    }
}