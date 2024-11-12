using System.Collections.Concurrent;
using Newtonsoft.Json;
using RDPSecure.Logging;

namespace RDPSecure.Data
{
    public class LoginAttemptsManager : IDisposable
    {
        private readonly string _attemptsFilePath;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _attempts;
        private readonly ISecurityLogger _logger;
        private readonly int _timeWindowMinutes;
        private readonly object _fileLock = new object();
        private readonly System.Threading.Timer _saveTimer;
        private readonly System.Threading.Timer _cleanupTimer;
        private volatile bool _disposed;
        private readonly SemaphoreSlim _saveThrottle;
        private readonly TimeSpan _saveThrottleInterval = TimeSpan.FromSeconds(5);
        private DateTime _lastSaveTime = DateTime.MinValue;

        public LoginAttemptsManager(ISecurityLogger logger, int timeWindowMinutes)
        {
            _logger = logger;
            _timeWindowMinutes = timeWindowMinutes;
            _attempts = new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>(StringComparer.OrdinalIgnoreCase);
            _attemptsFilePath = Path.Combine(AppConfig.AppDataPath, "login_attempts.json");
            _saveThrottle = new SemaphoreSlim(1, 1);

            AppConfig.EnsureDirectoriesExist();
            LoadAttempts();

            // Save attempts every 30 seconds
            _saveTimer = new System.Threading.Timer(
                _ => SaveAttemptsAsync().Wait(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30)
            );

            // Clean old attempts every 15 minutes
            _cleanupTimer = new System.Threading.Timer(
                _ => CleanAllOldAttempts(),
                null,
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(15)
            );
        }

        public Dictionary<string, List<DateTime>> GetAllAttempts()
        {
            return _attempts.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            );
        }

        public void AddAttempt(string ipAddress, DateTime timestamp)
        {
            var queue = _attempts.GetOrAdd(ipAddress, _ => new ConcurrentQueue<DateTime>());
            queue.Enqueue(timestamp);

            // Throttled save
            ThrottledSaveAsync().ConfigureAwait(false);
        }

        private async Task ThrottledSaveAsync()
        {
            if (await _saveThrottle.WaitAsync(0))
            {
                try
                {
                    var timeSinceLastSave = DateTime.UtcNow - _lastSaveTime;
                    if (timeSinceLastSave < _saveThrottleInterval)
                    {
                        await Task.Delay(_saveThrottleInterval - timeSinceLastSave);
                    }

                    await SaveAttemptsAsync();
                    _lastSaveTime = DateTime.UtcNow;
                }
                finally
                {
                    _saveThrottle.Release();
                }
            }
        }

        public int GetTotalAttempts(TimeSpan window)
        {
            var cutoffTime = DateTime.Now.Subtract(window);
            return _attempts.Sum(kvp => kvp.Value.Count(t => t > cutoffTime));
        }

        public int GetRecentAttemptCount(string ipAddress)
        {
            if (_attempts.TryGetValue(ipAddress, out var queue))
            {
                var cutoffTime = DateTime.Now.AddMinutes(-_timeWindowMinutes);
                return queue.Count(t => t > cutoffTime);
            }
            return 0;
        }

        public void LoadAttempts()
        {
            try
            {
                if (File.Exists(_attemptsFilePath))
                {
                    lock (_fileLock)
                    {
                        string json = File.ReadAllText(_attemptsFilePath);
                        var loadedAttempts = JsonConvert.DeserializeObject<Dictionary<string, List<DateTime>>>(json);

                        if (loadedAttempts != null)
                        {
                            _attempts.Clear();
                            var cutoffTime = DateTime.Now.AddHours(-48); // Keep 48 hours of history

                            foreach (var kvp in loadedAttempts)
                            {
                                var validAttempts = kvp.Value.Where(t => t > cutoffTime).ToList();
                                if (validAttempts.Any())
                                {
                                    var queue = new ConcurrentQueue<DateTime>(validAttempts);
                                    _attempts[kvp.Key] = queue;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading attempts: {ex.Message}");
            }
        }

        public async Task SaveAttemptsAsync()
        {
            if (_disposed) return;

            try
            {
                var attemptsToSave = _attempts.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                );

                var json = JsonConvert.SerializeObject(attemptsToSave, Formatting.Indented);
                await File.WriteAllTextAsync(_attemptsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving attempts: {ex.Message}");
            }
        }

        private void CleanAllOldAttempts()
        {
            try
            {
                var cutoffTime = DateTime.Now.AddHours(-48);
                foreach (var queue in _attempts.Values)
                {
                    while (queue.TryPeek(out DateTime oldest) && oldest <= cutoffTime)
                    {
                        queue.TryDequeue(out _);
                    }
                }

                // Remove empty queues
                var emptyKeys = _attempts.Where(kvp => !kvp.Value.Any())
                                       .Select(kvp => kvp.Key)
                                       .ToList();

                foreach (var key in emptyKeys)
                {
                    _attempts.TryRemove(key, out _);
                }

                if (emptyKeys.Any())
                {
                    SaveAttemptsAsync().Wait();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning old attempts: {ex.Message}");
            }
        }

        public void RemoveAttempts(string ipAddress)
        {
            if (_attempts.TryRemove(ipAddress, out _))
            {
                SaveAttemptsAsync().Wait();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _saveTimer?.Dispose();
                _cleanupTimer?.Dispose();
                _saveThrottle?.Dispose();
                SaveAttemptsAsync().Wait();
            }
        }
    }
}