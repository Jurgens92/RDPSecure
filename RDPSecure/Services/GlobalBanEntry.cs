using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using RDPSecure.Logging;

namespace RDPSecure.Services
{
    public class GlobalBanEntry
    {
        public string IPAddress { get; set; } = string.Empty;
        public DateTime BanTime { get; set; }
        public DateTime ExpiryTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string BannedBy { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
    }

    public class GlobalBanService
    {
        private readonly HttpClient _httpClient;
        private readonly ISecurityLogger _logger;
        private readonly string _owner;
        private readonly string _repo;
        private readonly string _path;
        private DateTime _lastRefreshTime;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30);
        private Dictionary<string, GlobalBanEntry> _cachedBans = new();
        private readonly object _cacheLock = new object();
        private string? _lastKnownSha;

        public GlobalBanService(ISecurityLogger logger, string? githubToken = null)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.github.com")
            };

            // Set up GitHub API headers
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("RDPSecure", "1.0"));

            if (!string.IsNullOrEmpty(githubToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", githubToken);
            }

            _logger = logger;
            _owner = "Jurgens92";
            _repo = "RDPSecureUpdates";
            _path = "BannedIPs.json";
            _lastRefreshTime = DateTime.MinValue;
        }

        public async Task<bool> TestConnection()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/repos/{_owner}/{_repo}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error testing GitHub connection", ex);
                return false;
            }
        }

        public async Task AddBan(string ipAddress, string location, int attemptCount)
        {
            try
            {
                await EnsureCacheIsValid();

                var newBan = new GlobalBanEntry
                {
                    IPAddress = ipAddress,
                    BanTime = DateTime.UtcNow,
                    ExpiryTime = DateTime.UtcNow.AddHours(24),
                    Location = location,
                    BannedBy = Environment.MachineName,
                    AttemptCount = attemptCount
                };

                var currentBans = await GetCurrentBans();
                currentBans.RemoveAll(b => b.ExpiryTime <= DateTime.UtcNow || b.IPAddress == ipAddress);
                currentBans.Add(newBan);

                await UpdateBanList(currentBans);

                lock (_cacheLock)
                {
                    _cachedBans[ipAddress] = newBan;
                }

                _logger.LogInformation($"Successfully added {ipAddress} to global banlist");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding {ipAddress} to global banlist", ex);
                throw;
            }
        }

        public async Task<List<GlobalBanEntry>> GetCurrentBans()
        {
            await EnsureCacheIsValid();

            lock (_cacheLock)
            {
                return _cachedBans.Values
                    .Where(b => b.ExpiryTime > DateTime.UtcNow)
                    .ToList();
            }
        }

        public async Task<bool> IsIPGloballyBanned(string ipAddress)
        {
            await EnsureCacheIsValid();

            lock (_cacheLock)
            {
                return _cachedBans.TryGetValue(ipAddress, out var ban) &&
                       ban.ExpiryTime > DateTime.UtcNow;
            }
        }

        private async Task EnsureCacheIsValid()
        {
            if (DateTime.Now - _lastRefreshTime > _refreshInterval)
            {
                await RefreshCache();
            }
        }

        private async Task RefreshCache()
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/repos/{_owner}/{_repo}/contents/{_path}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"GitHub API error: {await response.Content.ReadAsStringAsync()}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var githubContent = JsonConvert.DeserializeObject<GitHubContent>(content);

                if (githubContent == null || string.IsNullOrEmpty(githubContent.content))
                {
                    throw new Exception("Invalid content received from GitHub");
                }

                _lastKnownSha = githubContent.sha;

                var contentBytes = Convert.FromBase64String(githubContent.content);
                var jsonContent = Encoding.UTF8.GetString(contentBytes);
                var bans = JsonConvert.DeserializeObject<List<GlobalBanEntry>>(jsonContent)
                    ?? new List<GlobalBanEntry>();

                lock (_cacheLock)
                {
                    _cachedBans = bans.ToDictionary(b => b.IPAddress, b => b);
                    _lastRefreshTime = DateTime.Now;
                }

                _logger.LogInformation("Successfully refreshed global ban list cache");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing ban list cache", ex);
                throw;
            }
        }

        private async Task UpdateBanList(List<GlobalBanEntry> bans)
        {
            try
            {
                var content = JsonConvert.SerializeObject(bans, Formatting.Indented);
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var base64Content = Convert.ToBase64String(contentBytes);

                var updateRequest = new GitHubUpdateRequest
                {
                    message = $"Update ban list - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}",
                    content = base64Content,
                    sha = _lastKnownSha ?? string.Empty
                };

                var jsonContent = JsonConvert.SerializeObject(updateRequest);
                var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    $"/repos/{_owner}/{_repo}/contents/{_path}",
                    requestContent
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"GitHub API error: {await response.Content.ReadAsStringAsync()}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<GitHubContent>(responseContent);
                if (responseData != null)
                {
                    _lastKnownSha = responseData.sha;
                }

                _logger.LogInformation("Successfully updated global ban list on GitHub");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating ban list on GitHub", ex);
                throw;
            }
        }

        private class GitHubContent
        {
            public string sha { get; set; } = string.Empty;
            public string content { get; set; } = string.Empty;
            public string encoding { get; set; } = string.Empty;
        }

        private class GitHubUpdateRequest
        {
            public string message { get; set; } = string.Empty;
            public string content { get; set; } = string.Empty;
            public string sha { get; set; } = string.Empty;
        }
    }
}