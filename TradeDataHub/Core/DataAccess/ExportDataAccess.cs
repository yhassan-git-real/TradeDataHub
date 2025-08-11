using System;
using System.Data;
using Microsoft.Data.SqlClient;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Features.Export;
using Microsoft.Extensions.Configuration;
using System.IO;
using TradeDataHub.Core.Database;
using TradeDataHub.Core.Services;
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
            // Use cached configuration loading for better performance
            _exportSettings = ConfigurationCacheService.GetExportSettings();
            _dbSettings = ConfigurationCacheService.GetSharedDatabaseSettings();
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
                
                // Execute stored procedure using parameterized query for better performance and security
                using (var cmd = new SqlCommand(effectiveStoredProcedureName, con))
                {
                    currentCommand = cmd;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = _dbSettings.CommandTimeoutSeconds; // Use configurable timeout for long-running operations

                    // Add parameters with correct names matching the stored procedure
                    cmd.Parameters.AddWithValue("@fromMonth", fromMonth);
                    cmd.Parameters.AddWithValue("@ToMonth", toMonth);
                    cmd.Parameters.AddWithValue("@hs", hsCode);
                    cmd.Parameters.AddWithValue("@prod", product);
                    cmd.Parameters.AddWithValue("@Iec", iec);
                    cmd.Parameters.AddWithValue("@ExpCmp", exporter);
                    cmd.Parameters.AddWithValue("@forcount", country);
                    cmd.Parameters.AddWithValue("@forname", name);
                    cmd.Parameters.AddWithValue("@port", port);

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
                    countCmd.CommandTimeout = _dbSettings.CommandTimeoutSeconds; // Use configurable timeout for long-running operations

                    using var registration = cancellationToken.Register(() => 
                    {
                        CancellationCleanupHelper.SafeCancelCommand(currentCommand);
                    });

                    recordCount = Convert.ToInt64(countCmd.ExecuteScalar());
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var dataCmd = new SqlCommand($"SELECT * FROM {effectiveViewName} ORDER BY [{effectiveOrderByColumn}]", con);
                currentCommand = dataCmd;
                dataCmd.CommandTimeout = _dbSettings.CommandTimeoutSeconds; // Use configurable timeout for long-running operations

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
