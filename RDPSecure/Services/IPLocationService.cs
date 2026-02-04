using System.Net.Http;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using RDPSecure.Logging;

namespace RDPSecure.Services
{
    public class IPLocationService
    {
        // Static HttpClient to avoid socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Simple cache to avoid repeated lookups
        private static readonly ConcurrentDictionary<string, (string Location, DateTime CachedAt)> _cache = new();
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

        // Rate limiting - ip-api.com allows 45 requests per minute
        private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;
        private static readonly TimeSpan MinRequestInterval = TimeSpan.FromMilliseconds(1500); // ~40 requests per minute max

        private readonly ISecurityLogger _logger;
        private const string API_URL = "http://ip-api.com/json/";  // Free API, no key required

        public IPLocationService(ISecurityLogger logger)
        {
            _logger = logger;
        }

        public async Task<string> GetIPLocation(string ipAddress)
        {
            try
            {
                // Check cache first
                if (_cache.TryGetValue(ipAddress, out var cached))
                {
                    if (DateTime.UtcNow - cached.CachedAt < CacheExpiry)
                    {
                        return cached.Location;
                    }
                    // Cache expired, remove it
                    _cache.TryRemove(ipAddress, out _);
                }

                // Rate limiting
                await _rateLimiter.WaitAsync();
                try
                {
                    var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                    if (timeSinceLastRequest < MinRequestInterval)
                    {
                        await Task.Delay(MinRequestInterval - timeSinceLastRequest);
                    }
                    _lastRequestTime = DateTime.UtcNow;
                }
                finally
                {
                    _rateLimiter.Release();
                }

                var response = await _httpClient.GetStringAsync($"{API_URL}{ipAddress}").ConfigureAwait(false);
                var data = JObject.Parse(response);

                string location = "Unknown";

                if (data["status"]?.ToString() == "success")
                {
                    string country = data["country"]?.ToString() ?? "";
                    string city = data["city"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country))
                    {
                        location = $"{city}, {country}";
                    }
                    else if (!string.IsNullOrEmpty(country))
                    {
                        location = country;
                    }
                }

                // Cache the result
                _cache[ipAddress] = (location, DateTime.UtcNow);

                return location;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting location for IP {ipAddress}: {ex.Message}");
                return "Unknown";
            }
        }
    }
}
