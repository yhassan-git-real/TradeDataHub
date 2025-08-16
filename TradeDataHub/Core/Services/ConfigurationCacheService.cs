using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Configuration;
using TradeDataHub.Config;
using TradeDataHub.Core.Database;
using TradeDataHub.Features.Export;
using TradeDataHub.Features.Import;

namespace TradeDataHub.Core.Services
{
    /// <summary>
    /// Service for caching configuration objects to improve performance
    /// </summary>
    public class ConfigurationCacheService
    {
        private static readonly ConcurrentDictionary<string, object> _configCache = new();
        private static readonly object _lockObject = new();

        /// <summary>
        /// Get or load ExcelFormatSettings with caching
        /// </summary>
        public static ExcelFormatSettings GetExcelFormatSettings()
        {
            const string cacheKey = "ExcelFormatSettings";
            return (ExcelFormatSettings)_configCache.GetOrAdd(cacheKey, _ => LoadExcelFormatSettings());
        }

        /// <summary>
        /// Get or load ExportSettings with caching
        /// </summary>
        public static ExportSettings GetExportSettings()
        {
            const string cacheKey = "ExportSettings";
            return (ExportSettings)_configCache.GetOrAdd(cacheKey, _ => LoadExportSettings());
        }

        /// <summary>
        /// Get or load ImportSettings with caching
        /// </summary>
        public static ImportSettings GetImportSettings()
        {
            const string cacheKey = "ImportSettings";
            return (ImportSettings)_configCache.GetOrAdd(cacheKey, _ => LoadImportSettings());
        }

        /// <summary>
        /// Get or load SharedDatabaseSettings with caching
        /// </summary>
        public static SharedDatabaseSettings GetSharedDatabaseSettings()
        {
            const string cacheKey = "SharedDatabaseSettings";
            return (SharedDatabaseSettings)_configCache.GetOrAdd(cacheKey, _ => LoadSharedDatabaseSettings());
        }

        /// <summary>
        /// Get or load ImportExcelFormatSettings with caching
        /// </summary>
        public static ImportExcelFormatSettings GetImportExcelFormatSettings()
        {
            const string cacheKey = "ImportExcelFormatSettings";
            return (ImportExcelFormatSettings)_configCache.GetOrAdd(cacheKey, _ => LoadImportExcelFormatSettings());
        }

        /// <summary>
        /// Clear configuration cache (useful for testing or configuration updates)
        /// </summary>
        public static void ClearCache()
        {
            lock (_lockObject)
            {
                _configCache.Clear();
            }
        }

        /// <summary>
        /// Remove specific configuration from cache
        /// </summary>
        public static void InvalidateCache(string cacheKey)
        {
            _configCache.TryRemove(cacheKey, out _);
        }

        #region Private Loading Methods

        private static ExcelFormatSettings LoadExcelFormatSettings()
        {
            const string jsonFileName = "Config/ExportExcelFormatSettings.json";
            if (!File.Exists(jsonFileName))
                throw new FileNotFoundException($"Excel formatting file '{jsonFileName}' not found.");
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(jsonFileName, false);
            var config = builder.Build();
            return config.Get<ExcelFormatSettings>()!;
        }

        private static ExportSettings LoadExportSettings()
        {
            const string json = "Config/export.appsettings.json";
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<ExportSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind ExportSettingsRoot");
            return root.ExportSettings;
        }

        private static ImportSettings LoadImportSettings()
        {
            const string json = "Config/import.appsettings.json";
            if (!File.Exists(json)) throw new FileNotFoundException($"Missing import settings file: {json}");
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<ImportSettingsRoot>();
            if (root == null) throw new InvalidOperationException("Failed to bind ImportSettingsRoot");
            return root.ImportSettings;
        }

        private static SharedDatabaseSettings LoadSharedDatabaseSettings()
        {
            const string json = "Config/database.appsettings.json";
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<SharedDatabaseSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
            return root.DatabaseConfig;
        }

        private static ImportExcelFormatSettings LoadImportExcelFormatSettings()
        {
            const string json = "Config/ImportExcelFormatSettings.json";
            if (!File.Exists(json)) throw new FileNotFoundException($"Missing import formatting file: {json}");
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(json, false);
            var cfg = builder.Build();
            return cfg.Get<ImportExcelFormatSettings>()!;
        }

        #endregion
    }
}
