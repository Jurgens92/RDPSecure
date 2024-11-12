using Newtonsoft.Json;
using RDPSecure.Logging;
using System;
using System.Collections.Concurrent;

namespace RDPSecure.Data
{
    public class LoginAttemptsManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _attempts;
        private readonly string _attemptsFilePath;
        private readonly object _fileLock = new object();
        private readonly ISecurityLogger _logger;
        private readonly int _timeWindowMinutes;
        private readonly System.Threading.Timer _saveTimer;
        private readonly System.Threading.Timer _cleanupTimer;
        private bool _disposed;

        public LoginAttemptsManager(ISecurityLogger logger, int timeWindowMinutes)
        {
            _logger = logger;
            _timeWindowMinutes = timeWindowMinutes;
            _attempts = new ConcurrentDictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);
            _attemptsFilePath = Path.Combine(AppConfig.AppDataPath, "login_attempts.json");

            // Load existing attempts - modified to keep a longer history
            LoadAttempts();

            // Save attempts every 30 seconds instead of 1 minute
            _saveTimer = new System.Threading.Timer(
                _ => SaveAttempts(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30)
            );

            // Clean old attempts every 15 minutes instead of 5
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
                kvp =>
                {
                    lock (kvp.Value)
                    {
                        return kvp.Value.ToList();
                    }
                }
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
            int total = 0;

            foreach (var kvp in _attempts)
            {
                lock (kvp.Value)
                {
                    total += kvp.Value.Count(t => t > cutoffTime);
                }
            }

            return total;
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
                if (File.Exists(_attemptsFilePath))
                {
                    lock (_fileLock)
                    {
                        string json = File.ReadAllText(_attemptsFilePath);
                        var loadedAttempts = JsonConvert.DeserializeObject<Dictionary<string, List<DateTime>>>(json);

                        if (loadedAttempts != null)
                        {
                            _attempts.Clear();

                            // Keep attempts from the last 48 hours instead of 24
                            var cutoffTime = DateTime.Now.AddHours(-48);
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
                    Directory.CreateDirectory(Path.GetDirectoryName(_attemptsFilePath)!);

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
                    File.WriteAllText(_attemptsFilePath, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving login attempts: {ex.Message}");
            }
        }

        private void CleanAllOldAttempts()
        {
            try
            {
                // Keep attempts from last 48 hours for history
                var cutoffTime = DateTime.Now.AddHours(-48);
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
                var emptyKeys = _attempts.Where(kvp => !kvp.Value.Any())
                                       .Select(kvp => kvp.Key)
                                       .ToList();
                foreach (var key in emptyKeys)
                {
                    _attempts.TryRemove(key, out _);
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    SaveAttempts();
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