using Newtonsoft.Json;
using RDPSecure.Logging;
using System;
using System.Collections.Concurrent;

namespace RDPSecure.Data
{
    public class LoginAttemptsManager : IDisposable
    {
        private static readonly string AttemptsFilePath = Path.Combine(
            AppConfig.AppDataPath,
            "login_attempts.json"
        );

        private readonly ConcurrentDictionary<string, List<DateTime>> _attempts;
        private readonly object _fileLock = new object();
        private readonly System.Threading.Timer _saveTimer;
        private readonly ISecurityLogger _logger;
        private readonly int _timeWindowMinutes;
        private bool _disposed;
        private readonly System.Threading.Timer _cleanupTimer;

        public LoginAttemptsManager(ISecurityLogger logger, int timeWindowMinutes)
        {
            _logger = logger;
            _timeWindowMinutes = timeWindowMinutes;
            _attempts = new ConcurrentDictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);

            // Load existing attempts
            LoadAttempts();

            // Setup timer to periodically save attempts (every 1 minute)
            _saveTimer = new System.Threading.Timer(
                _ => SaveAttempts(),
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1)
            );

            // Setup timer to periodically clean old attempts (every 5 minutes)
            _cleanupTimer = new System.Threading.Timer(
                _ => CleanAllOldAttempts(),
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5)
            );
        }

        public void AddAttempt(string ipAddress, DateTime timestamp)
        {
            _attempts.AddOrUpdate(
                ipAddress,
                new List<DateTime> { timestamp },
                (key, existingList) =>
                {
                    lock (existingList)
                    {
                        existingList.Add(timestamp);
                        return existingList;
                    }
                });

            // Save immediately after adding
            SaveAttempts();
        }

        public int GetTotalAttempts(TimeSpan window)
        {
            var cutoffTime = DateTime.Now.Subtract(window);
            return _attempts.Sum(kvp =>
            {
                lock (kvp.Value)
                {
                    return kvp.Value.Count(t => t > cutoffTime);
                }
            });
        }

        public int GetRecentAttemptCount(string ipAddress)
        {
            if (_attempts.TryGetValue(ipAddress, out var attempts))
            {
                var cutoffTime = DateTime.Now.AddMinutes(-_timeWindowMinutes);
                lock (attempts)
                {
                    return attempts.Count(t => t > cutoffTime);
                }
            }
            return 0;
        }

        private void LoadAttempts()
        {
            try
            {
                if (File.Exists(AttemptsFilePath))
                {
                    lock (_fileLock)
                    {
                        string json = File.ReadAllText(AttemptsFilePath);
                        var loadedAttempts = JsonConvert.DeserializeObject<Dictionary<string, List<DateTime>>>(json);

                        if (loadedAttempts != null)
                        {
                            // Clear existing attempts first
                            _attempts.Clear();

                            // Only load attempts within the time window
                            var cutoffTime = DateTime.Now.AddHours(-24);
                            foreach (var kvp in loadedAttempts)
                            {
                                var validAttempts = kvp.Value.Where(t => t > cutoffTime).ToList();
                                if (validAttempts.Any())
                                {
                                    _attempts[kvp.Key] = validAttempts;
                                }
                            }

                            _logger.LogInformation($"Loaded {_attempts.Count} IPs with recent attempts");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading login attempts: {ex.Message}");
            }
        }

        private void SaveAttempts()
        {
            try
            {
                lock (_fileLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(AttemptsFilePath)!);

                    var attemptsSnapshot = _attempts.ToDictionary(
                        kvp => kvp.Key,
                        kvp =>
                        {
                            lock (kvp.Value)
                            {
                                return kvp.Value.ToList();
                            }
                        }
                    );

                    string json = JsonConvert.SerializeObject(attemptsSnapshot, Formatting.Indented);
                    File.WriteAllText(AttemptsFilePath, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving login attempts: {ex.Message}");
            }
        }

        private void CleanAllOldAttempts()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-_timeWindowMinutes);
            bool hasChanges = false;

            foreach (var ip in _attempts.Keys)
            {
                if (_attempts.TryGetValue(ip, out var attempts))
                {
                    lock (attempts)
                    {
                        int initialCount = attempts.Count;
                        attempts.RemoveAll(t => t <= cutoffTime);
                        if (attempts.Count != initialCount)
                        {
                            hasChanges = true;
                        }
                    }
                }
            }

            // Remove empty entries
            var emptyKeys = _attempts.Where(kvp => !kvp.Value.Any()).Select(kvp => kvp.Key).ToList();
            foreach (var key in emptyKeys)
            {
                _attempts.TryRemove(key, out _);
                hasChanges = true;
            }

            // Save if there were any changes
            if (hasChanges)
            {
                SaveAttempts();
            }
        }

        public void RemoveAttempts(string ipAddress)
        {
            if (_attempts.TryRemove(ipAddress, out _))
            {
                SaveAttempts();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _saveTimer?.Dispose();
                    _cleanupTimer?.Dispose();
                    SaveAttempts(); // Final save on disposal
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}