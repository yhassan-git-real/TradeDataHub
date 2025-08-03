using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;

namespace TradeDataHub.Core
{
    public class DataAccess
    {
        private readonly LoggingHelper _logger;

        public DataAccess()
        {
            _logger = LoggingHelper.Instance;
        }

        public bool TestConnection()
        {
            try
            {
                var connectionString = App.Settings.Database.ConnectionString;
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    _logger.LogInfo("Database connection test successful");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Database connection test failed", ex);
                MessageBox.Show($"Connection test failed: {ex.Message}", "Connection Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        public (SqlConnection connection, SqlDataReader reader, long recordCount) GetDataReader(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            SqlConnection? con = null;
            SqlDataReader? reader = null;
            
            try
            {
                var connectionString = App.Settings.Database.ConnectionString;
                var storedProcName = App.Settings.Database.StoredProcedureName;

                con = new SqlConnection(connectionString);
                con.Open();
                
                // Execute stored procedure
                using (var cmd = new SqlCommand(storedProcName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 50000;
                    
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_FROM_MONTH, int.Parse(fromMonth));
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_TO_MONTH, int.Parse(toMonth));
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_HS_CODE, hsCode);
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_PRODUCT, product);
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_IEC, iec);
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_EXPORTER, exporter);
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_FOREIGN_COUNTRY, country);
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_FOREIGN_NAME, name);
                    cmd.Parameters.AddWithValue(ParameterHelper.StoredProcedureParameters.SP_PORT, port);

                    cmd.ExecuteNonQuery();
                }
                
                // Get record count
                long recordCount = 0;
                var viewName = App.Settings.Database.ViewName;
                using (var countCmd = new SqlCommand($"SELECT COUNT(*) FROM {viewName}", con))
                {
                    recordCount = Convert.ToInt64(countCmd.ExecuteScalar());
                }
                
                // Open recordset for streaming
                var orderByColumn = App.Settings.Database.OrderByColumn;
                var dataCmd = new SqlCommand($"SELECT * FROM {viewName} ORDER BY [{orderByColumn}]", con);
                dataCmd.CommandTimeout = 50000;
                reader = dataCmd.ExecuteReader();
                
                return (con, reader, recordCount);
            }
            catch (SqlException ex)
            {
                reader?.Dispose();
                con?.Dispose();
                
                string errorMessage = ex.Number switch
                {
                    2 => "Cannot connect to SQL Server. Please check if the server is running and accessible.",
                    18456 => "Login failed. Please check your username and password.", 
                    4060 => "Cannot open database. Please check if the database exists.",
                    -2 => "Connection timeout. Please check your network connection.",
                    _ => $"A database error occurred: {ex.Message}"
                };
                _logger.LogError($"SQL Server error (Code: {ex.Number}): {errorMessage}", ex);
                MessageBox.Show(errorMessage, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                reader?.Dispose();
                con?.Dispose();
                _logger.LogError("Unexpected error during data access", ex);
                MessageBox.Show($"An unexpected error occurred during data access: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }
}
