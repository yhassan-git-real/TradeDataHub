using System;
using System.Data;
using Microsoft.Data.SqlClient;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Core.Database;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using TradeDataHub.Core.Cancellation;

namespace TradeDataHub.Features.Import
{
    public class ImportDataAccess
    {
        private readonly LoggingHelper _logger;
    private readonly ImportSettings _settings;
    private readonly SharedDatabaseSettings _dbSettings;

        public ImportDataAccess(ImportSettings settings)
        {
            _logger = LoggingHelper.Instance;
            _settings = settings;
            _dbSettings = LoadSharedDatabaseSettings();
        }

        private SharedDatabaseSettings LoadSharedDatabaseSettings()
        {
            const string json = "Config/database.appsettings.json";
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<SharedDatabaseSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
            return root.DatabaseConfig;
        }

        public (SqlConnection connection, SqlDataReader reader, long recordCount) GetDataReader(
            string fromMonth, string toMonth, string hsCode, string product, string iec, string importer, string country, string name, string port, CancellationToken cancellationToken = default, string? viewName = null, string? storedProcedureName = null)
        {
            SqlConnection? con = null;
            SqlDataReader? reader = null;
            SqlCommand? currentCommand = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                con = new SqlConnection(_dbSettings.ConnectionString);
                con.Open();

                cancellationToken.ThrowIfCancellationRequested();

                // Determine effective view, stored procedure, and order by column
                string effectiveStoredProcedureName = storedProcedureName ?? _settings.Database.StoredProcedureName;
                string effectiveViewName = viewName ?? _settings.Database.ViewName;
                string effectiveOrderByColumn = _settings.Database.OrderByColumn;
                
                // If using a custom view from ImportObjects, get its OrderByColumn
                if (viewName != null && _settings.ImportObjects != null)
                {
                    var customView = _settings.ImportObjects.Views?.FirstOrDefault(v => v.Name == viewName);
                    if (customView != null && !string.IsNullOrEmpty(customView.OrderByColumn))
                    {
                        effectiveOrderByColumn = customView.OrderByColumn;
                    }
                }
                
                // Execute stored procedure - Use string formatting like the VB implementation
                // This matches the legacy VB code's approach where SQL was constructed as a string
                string sqlCommand = $"EXEC {effectiveStoredProcedureName} {fromMonth}, {toMonth}, '{hsCode}', '{product}', '{iec}', '{importer}', '{country}', '{name}', '{port}'";
                
                using (var cmd = new SqlCommand(sqlCommand, con))
                {
                    currentCommand = cmd;
                    cmd.CommandType = CommandType.Text; // Changed to text since we're using string formatting
                    cmd.CommandTimeout = 50000;

                    // Register cancellation callback to cancel the command
                    using var registration = cancellationToken.Register(() => 
                    {
                        CancellationCleanupHelper.SafeCancelCommand(currentCommand);
                    });

                    cmd.ExecuteNonQuery();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                currentCommand = null; // Command completed successfully

                // Row count
                long recordCount = 0;
                using (var countCmd = new SqlCommand($"SELECT COUNT(*) FROM {effectiveViewName}", con))
                {
                    currentCommand = countCmd;
                    countCmd.CommandTimeout = 50000;

                    using var registration = cancellationToken.Register(() => 
                    {
                        CancellationCleanupHelper.SafeCancelCommand(currentCommand);
                    });

                    recordCount = Convert.ToInt64(countCmd.ExecuteScalar());
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Open streaming reader
                var dataCmd = new SqlCommand($"SELECT * FROM {effectiveViewName} ORDER BY [{effectiveOrderByColumn}]", con);
                currentCommand = dataCmd;
                dataCmd.CommandTimeout = 50000;

                using var dataRegistration = cancellationToken.Register(() => 
                {
                    CancellationCleanupHelper.SafeCancelCommand(currentCommand);
                });

                reader = dataCmd.ExecuteReader();
                cancellationToken.ThrowIfCancellationRequested();

                return (con, reader, recordCount);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                CancellationCleanupHelper.SafeDisposeReader(reader);
                CancellationCleanupHelper.SafeDisposeConnection(con);
                throw;
            }
            catch
            {
                reader?.Dispose();
                con?.Dispose();
                throw;
            }
        }
    }
}
