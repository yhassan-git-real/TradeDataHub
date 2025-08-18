using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TradeDataHub.Config;
using TradeDataHub.Core.Services;

namespace TradeDataHub.Core.Logging
{
    /// <summary>
    /// High-performance string pooling for logging to reduce allocations
    /// </summary>
    public sealed class StringPool
    {
        private static readonly Lazy<StringPool> _instance = new(() => new StringPool());
        public static StringPool Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, string> _stringPool = new();
        private readonly ConcurrentDictionary<string, string> _templatePool = new();
        private readonly PerformanceSettings _performanceSettings;

        // Pre-cached common log patterns
        private readonly string[] _commonPrefixes = {
            "🚀 PROCESS START:",
            "✅ PROCESS COMPLETE:",
            "➤ Database:",
            "➤ Excel Creation:",
            "➤ File Save:",
            "➤ Validation:",
            "➤ Stored Procedure:",
            "💾 File Save Completed",
            "📊 Excel Complete:",
            "⏱️ Total Time:",
            "📋 Parameters:",
            "Creating Excel file:",
            "Row count:",
            "Progress:",
            "Completed chunked processing:",
            "Starting chunked processing:"
        };

        private readonly string[] _commonSuffixes = {
            " rows",
            " records",
            "Success",
            "Failed",
            "Completed",
            "Started",
            "Cancelled",
            "Error",
            "Warning",
            "Info"
        };

        private StringPool()
        {
            _performanceSettings = ConfigurationCacheService.GetPerformanceSettings();
            PreCacheCommonStrings();
        }

        /// <summary>
        /// Pre-cache commonly used log strings
        /// </summary>
        private void PreCacheCommonStrings()
        {
            // Cache common prefixes
            foreach (var prefix in _commonPrefixes)
            {
                _stringPool.TryAdd(prefix, prefix);
            }

            // Cache common suffixes
            foreach (var suffix in _commonSuffixes)
            {
                _stringPool.TryAdd(suffix, suffix);
            }

            // Cache common log separators
            _stringPool.TryAdd(" | ", " | ");
            _stringPool.TryAdd(" - ", " - ");
            _stringPool.TryAdd(": ", ": ");
            _stringPool.TryAdd("================================================================================", "================================================================================");
            _stringPool.TryAdd("--------------------------------------------------------------------------------", "--------------------------------------------------------------------------------");
            
            // Cache common status messages
            _stringPool.TryAdd("No data - skipped", "No data - skipped");
            _stringPool.TryAdd("Excel row limit exceeded", "Excel row limit exceeded");
            _stringPool.TryAdd("Process started", "Process started");
            _stringPool.TryAdd("Process completed", "Process completed");
            _stringPool.TryAdd("Process failed", "Process failed");
            _stringPool.TryAdd("Cancelled by user", "Cancelled by user");
        }

        /// <summary>
        /// Get a pooled string, caching it if not already present
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetPooled(string value)
        {
            if (!_performanceSettings.Logging.EnableStringPooling || string.IsNullOrEmpty(value))
                return value;

            return _stringPool.GetOrAdd(value, v => v);
        }

        /// <summary>
        /// Get a pooled formatted string with reduced allocations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetPooledFormat(string template, object arg1)
        {
            if (!_performanceSettings.Logging.EnableStringPooling)
                return string.Format(template, arg1);

            var key = $"{template}|{arg1}";
            return _templatePool.GetOrAdd(key, k => string.Format(template, arg1));
        }

        /// <summary>
        /// Get a pooled formatted string with two arguments
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetPooledFormat(string template, object arg1, object arg2)
        {
            if (!_performanceSettings.Logging.EnableStringPooling)
                return string.Format(template, arg1, arg2);

            var key = $"{template}|{arg1}|{arg2}";
            return _templatePool.GetOrAdd(key, k => string.Format(template, arg1, arg2));
        }

        /// <summary>
        /// Get a pooled formatted string with three arguments
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetPooledFormat(string template, object arg1, object arg2, object arg3)
        {
            if (!_performanceSettings.Logging.EnableStringPooling)
                return string.Format(template, arg1, arg2, arg3);

            var key = $"{template}|{arg1}|{arg2}|{arg3}";
            return _templatePool.GetOrAdd(key, k => string.Format(template, arg1, arg2, arg3));
        }

        /// <summary>
        /// Create optimized progress message with pooled components
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateProgressMessage(int processed, int total, double percentage)
        {
            var template = GetPooled("Progress: {0:N0}/{1:N0} rows ({2:F1}%)");
            return GetPooledFormat(template, processed, total, percentage);
        }

        /// <summary>
        /// Create optimized timing message with pooled components
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateTimingMessage(string operation, TimeSpan elapsed)
        {
            var template = GetPooled("⏱️ {0}: {1:mm\\:ss\\.fff}");
            return GetPooledFormat(template, operation, elapsed);
        }

        /// <summary>
        /// Create optimized file result message
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateFileResultMessage(string fileName, TimeSpan elapsed, long recordCount)
        {
            var template = GetPooled("✅ Excel Complete: {0} | ⏱️ {1:mm\\:ss\\.fff} | 📊 {2:N0} records");
            return GetPooledFormat(template, fileName, elapsed, recordCount);
        }

        /// <summary>
        /// Create optimized process complete message
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateProcessCompleteMessage(string processName, TimeSpan elapsed, string result)
        {
            var template = GetPooled("✅ PROCESS COMPLETE: {0} | ⏱️ {1:mm\\:ss\\.fff} | 📊 {2}");
            return GetPooledFormat(template, processName, elapsed, result);
        }

        /// <summary>
        /// Create optimized step message
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateStepMessage(string stepName, string details)
        {
            var template = GetPooled("➤ {0}: {1}");
            return GetPooledFormat(template, stepName, details);
        }

        /// <summary>
        /// Create optimized row count message
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateRowCountMessage(long rowCount)
        {
            var template = GetPooled("Row count: {0:N0}");
            return GetPooledFormat(template, rowCount);
        }

        /// <summary>
        /// Create optimized chunked processing start message
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateChunkedStartMessage(long recordCount, int chunkSize)
        {
            var template = GetPooled("Starting chunked processing: {0:N0} rows, chunk size: {1:N0}");
            return GetPooledFormat(template, recordCount, chunkSize);
        }

        /// <summary>
        /// Create optimized chunked processing complete message
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string CreateChunkedCompleteMessage(int processedRows, int chunksProcessed)
        {
            var template = GetPooled("Completed chunked processing: {0:N0} rows in {1} chunks");
            return GetPooledFormat(template, processedRows, chunksProcessed);
        }

        /// <summary>
        /// Get pool statistics for monitoring
        /// </summary>
        public (int StringPoolSize, int TemplatePoolSize) GetStatistics()
        {
            return (_stringPool.Count, _templatePool.Count);
        }

        /// <summary>
        /// Clear cached strings to free memory
        /// </summary>
        public void ClearCache()
        {
            _stringPool.Clear();
            _templatePool.Clear();
            PreCacheCommonStrings(); // Re-cache essentials
        }

        /// <summary>
        /// Trim cache if it gets too large
        /// </summary>
        public void TrimCache(int maxSize = 10000)
        {
            if (_stringPool.Count + _templatePool.Count > maxSize)
            {
                // Clear template pool first as it's more dynamic
                _templatePool.Clear();
                
                // If still too large, clear and rebuild string pool
                if (_stringPool.Count > maxSize / 2)
                {
                    ClearCache();
                }
            }
        }
    }
}

