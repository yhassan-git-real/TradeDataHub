using System;
using System.Data;
using Microsoft.Data.SqlClient;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Features.Export;
using Microsoft.Extensions.Configuration;
using System.IO;
using TradeDataHub.Core.Database;
using System.Threading;
using TradeDataHub.Core.Cancellation;

namespace TradeDataHub.Core.DataAccess
{
    public class ExportDataAccess
    {
        private readonly LoggingHelper _logger;
        private readonly ExportSettings _exportSettings;
        private readonly SharedDatabaseSettings _dbSettings;

        public ExportDataAccess()
        {
            _logger = LoggingHelper.Instance;
            _exportSettings = LoadExportSettings();
            _dbSettings = LoadSharedDatabaseSettings();
        }

        private ExportSettings LoadExportSettings()
        {
            const string json = "Config/export.appsettings.json";
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json,false);
            var cfg = builder.Build();
            var root = cfg.Get<ExportSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind ExportSettingsRoot");
            return root.ExportSettings;
        }

        private SharedDatabaseSettings LoadSharedDatabaseSettings()
        {
            const string json = "Config/database.appsettings.json";
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json,false);
            var cfg = builder.Build();
            var root = cfg.Get<SharedDatabaseSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
            return root.DatabaseConfig;
        }

        public (SqlConnection connection, SqlDataReader reader, long recordCount) GetDataReader(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port, CancellationToken cancellationToken = default, string? viewName = null, string? storedProcedureName = null)
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

                string effectiveStoredProcedureName = storedProcedureName ?? _exportSettings.Operation.StoredProcedureName;
                string effectiveViewName = viewName ?? _exportSettings.Operation.ViewName;
                string effectiveOrderByColumn = _exportSettings.Operation.OrderByColumn;
                
                // If using a custom view from ExportObjects, get its OrderByColumn
                if (viewName != null && _exportSettings.ExportObjects != null)
                {
                    var customView = _exportSettings.ExportObjects.Views?.FirstOrDefault(v => v.Name == viewName);
                    if (customView != null && !string.IsNullOrEmpty(customView.OrderByColumn))
                    {
                        effectiveOrderByColumn = customView.OrderByColumn;
                    }
                }
                
                // Execute stored procedure - Use string formatting like the VB implementation
                // This matches the legacy VB code's approach where SQL was constructed as a string
                string sqlCommand = $"EXEC {effectiveStoredProcedureName} {fromMonth}, {toMonth}, '{hsCode}', '{product}', '{iec}', '{exporter}', '{country}', '{name}', '{port}'";
                
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
