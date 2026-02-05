using System.Collections.Concurrent;
using RDPSecure.Logging;

namespace RDPSecure.Data
{
    public class LoginAttemptsManager : IDisposable
    {
        private static DatabaseManager Database => DatabaseProvider.Instance;

        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _attempts;
        private readonly ISecurityLogger _logger;
        private int _timeWindowMinutes;
        private readonly System.Threading.Timer _cleanupTimer;
        private volatile bool _disposed;

        public LoginAttemptsManager(ISecurityLogger logger, int timeWindowMinutes)
        {
            _logger = logger;
            _timeWindowMinutes = timeWindowMinutes;
            _attempts = new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>(StringComparer.OrdinalIgnoreCase);

            AppConfig.EnsureDirectoriesExist();
            LoadAttempts();

            // Clean old attempts every 15 minutes
            _cleanupTimer = new System.Threading.Timer(
                _ => CleanAllOldAttempts(),
                null,
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(15)
            );
        }

        /// <summary>
        /// Updates the time window used for counting recent attempts.
        /// Call this when settings change.
        /// </summary>
        public void UpdateTimeWindow(int timeWindowMinutes)
        {
            _timeWindowMinutes = timeWindowMinutes;
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
            // Add to in-memory cache
            var queue = _attempts.GetOrAdd(ipAddress, _ => new ConcurrentQueue<DateTime>());
            queue.Enqueue(timestamp);

            // Persist to database immediately (SQLite is fast enough for this)
            try
            {
                Database.AddLoginAttempt(ipAddress, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving attempt to database: {ex.Message}");
            }
        }

        public int GetTotalAttempts(TimeSpan window)
        {
            try
            {
                return Database.GetTotalAttempts(window);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting total attempts: {ex.Message}");
                // Fall back to in-memory count
                var cutoffTime = DateTime.UtcNow.Subtract(window);
                return _attempts.Sum(kvp => kvp.Value.Count(t => t > cutoffTime));
            }
        }

        public int GetRecentAttemptCount(string ipAddress)
        {
            try
            {
                return Database.GetRecentAttemptCount(ipAddress, _timeWindowMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recent attempt count: {ex.Message}");
                // Fall back to in-memory count
                if (_attempts.TryGetValue(ipAddress, out var queue))
                {
                    var cutoffTime = DateTime.UtcNow.AddMinutes(-_timeWindowMinutes);
                    return queue.Count(t => t > cutoffTime);
                }
                return 0;
            }
        }

        public void LoadAttempts()
        {
            try
            {
                var loadedAttempts = Database.LoadLoginAttempts(TimeSpan.FromHours(48));

                // Merge loaded attempts with existing in-memory data instead of clearing
                // This prevents losing concurrent additions during load
                foreach (var kvp in loadedAttempts)
                {
                    if (kvp.Value.Any())
                    {
                        _attempts.AddOrUpdate(
                            kvp.Key,
                            _ => new ConcurrentQueue<DateTime>(kvp.Value),
                            (_, existingQueue) =>
                            {
                                // Merge: keep existing items and add any from DB that aren't already there
                                var existingSet = new HashSet<DateTime>(existingQueue);
                                foreach (var timestamp in kvp.Value)
                                {
                                    if (!existingSet.Contains(timestamp))
                                    {
                                        existingQueue.Enqueue(timestamp);
                                    }
                                }
                                return existingQueue;
                            }
                        );
                    }
                }

                // Remove keys that are no longer in the database and have empty queues
                var keysToRemove = _attempts.Keys
                    .Where(k => !loadedAttempts.ContainsKey(k) && !_attempts[k].Any())
                    .ToList();
                foreach (var key in keysToRemove)
                {
                    _attempts.TryRemove(key, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading attempts: {ex.Message}");
            }
        }

        public Task SaveAttemptsAsync()
        {
            // With SQLite, attempts are saved immediately on AddAttempt
            // This method is kept for compatibility but doesn't need to do anything
            return Task.CompletedTask;
        }

        private void CleanAllOldAttempts()
        {
            try
            {
                // Clean database
                Database.CleanupOldAttempts(TimeSpan.FromHours(48));

                // Clean in-memory cache
                var cutoffTime = DateTime.UtcNow.AddHours(-48);
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
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning old attempts: {ex.Message}");
            }
        }

        public void RemoveAttempts(string ipAddress)
        {
            _attempts.TryRemove(ipAddress, out _);
            try
            {
                Database.RemoveLoginAttempts(ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing attempts from database: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cleanupTimer?.Dispose();
            }
        }
    }
}
