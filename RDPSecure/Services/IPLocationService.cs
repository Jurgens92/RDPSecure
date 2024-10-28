using System.Net.Http;
using Newtonsoft.Json.Linq;
using RDPSecure.Logging;

namespace RDPSecure.Services
{
    public class IPLocationService
    {
        private readonly HttpClient _httpClient;
        private readonly ISecurityLogger _logger;
        private const string API_URL = "http://ip-api.com/json/";  // Free API, no key required

        public IPLocationService(ISecurityLogger logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<string> GetIPLocation(string ipAddress)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{API_URL}{ipAddress}");
                var data = JObject.Parse(response);

                if (data["status"]?.ToString() == "success")
                {
                    string country = data["country"]?.ToString() ?? "";
                    string city = data["city"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country))
                    {
                        return $"{city}, {country}";
                    }
                    else if (!string.IsNullOrEmpty(country))
                    {
                        return country;
                    }
                }

                return "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting location for IP {ipAddress}: {ex.Message}");
                return "Unknown";
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}