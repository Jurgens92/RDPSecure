namespace RDPSecure.Data
{
    /// <summary>
    /// Provides a single shared DatabaseManager instance for the entire application.
    /// This prevents multiple SQLite connections and potential locking issues.
    /// </summary>
    public static class DatabaseProvider
    {
        private static readonly Lazy<DatabaseManager> _instance = new(() => new DatabaseManager());

        /// <summary>
        /// Gets the shared DatabaseManager instance.
        /// </summary>
        public static DatabaseManager Instance => _instance.Value;

        /// <summary>
        /// Checks if the database has been initialized.
        /// </summary>
        public static bool IsInitialized => _instance.IsValueCreated;
    }
}
