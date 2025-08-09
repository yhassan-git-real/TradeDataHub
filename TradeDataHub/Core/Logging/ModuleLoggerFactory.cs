using System;
using System.Collections.Concurrent;

namespace TradeDataHub.Core.Logging
{
    /// <summary>
    /// Factory for creating and managing module-specific loggers
    /// </summary>
    public static class ModuleLoggerFactory
    {
        private static readonly ConcurrentDictionary<string, ModuleLogger> _moduleLoggers = new();

        /// <summary>
        /// Gets or creates a module-specific logger
        /// </summary>
        /// <param name="modulePrefix">The prefix for the log file (e.g., "Export_Log", "Import_Log")</param>
        /// <param name="logFileExtension">The file extension for the log file (default: ".txt")</param>
        /// <returns>A module-specific logger instance</returns>
        public static ModuleLogger GetLogger(string modulePrefix, string logFileExtension = ".txt")
        {
            if (string.IsNullOrWhiteSpace(modulePrefix))
                throw new ArgumentNullException(nameof(modulePrefix));

            var key = $"{modulePrefix}_{logFileExtension}";
            return _moduleLoggers.GetOrAdd(key, _ => new ModuleLogger(modulePrefix, logFileExtension));
        }

        /// <summary>
        /// Gets a logger for Export operations
        /// </summary>
        /// <returns>Export module logger</returns>
        public static ModuleLogger GetExportLogger()
        {
            return GetLogger("Export_Log", ".txt");
        }

        /// <summary>
        /// Gets a logger for Import operations
        /// </summary>
        /// <returns>Import module logger</returns>
        public static ModuleLogger GetImportLogger()
        {
            return GetLogger("Import_Log", ".txt");
        }

        /// <summary>
        /// Gets a logger for Cancellation operations
        /// </summary>
        /// <returns>Cancellation module logger</returns>
        public static ModuleLogger GetCancellationLogger()
        {
            return GetLogger("Cancellation_Log", ".txt");
        }

        /// <summary>
        /// Disposes all module loggers and clears the cache
        /// </summary>
        public static void DisposeAll()
        {
            foreach (var logger in _moduleLoggers.Values)
            {
                logger.Dispose();
            }
            _moduleLoggers.Clear();
        }
    }
}
