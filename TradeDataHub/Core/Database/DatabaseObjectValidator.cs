using System;
using System.Data;
using Microsoft.Data.SqlClient;
using TradeDataHub.Core.Logging;

namespace TradeDataHub.Core.Database
{
    /// <summary>
    /// Service for validating database objects against the actual database
    /// </summary>
    public class DatabaseObjectValidator
    {
        private readonly string _connectionString;
        private readonly LoggingHelper _logger;

        public DatabaseObjectValidator(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = LoggingHelper.Instance;
        }

        /// <summary>
        /// Checks if a view exists in the database
        /// </summary>
        /// <param name="viewName">The name of the view to check</param>
        /// <returns>True if the view exists, false otherwise</returns>
        public bool ViewExists(string viewName)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                return false;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Query to check if the view exists in the database
                string query = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.VIEWS 
                    WHERE TABLE_NAME = @ViewName";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ViewName", viewName);

                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking if view '{viewName}' exists: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Checks if a stored procedure exists in the database
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to check</param>
        /// <returns>True if the stored procedure exists, false otherwise</returns>
        public bool StoredProcedureExists(string storedProcedureName)
        {
            if (string.IsNullOrEmpty(storedProcedureName))
            {
                return false;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // Query to check if the stored procedure exists in the database
                string query = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.ROUTINES 
                    WHERE ROUTINE_TYPE = 'PROCEDURE' 
                    AND ROUTINE_NAME = @ProcedureName";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProcedureName", storedProcedureName);

                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking if stored procedure '{storedProcedureName}' exists: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Validates both a view and a stored procedure in a single call
        /// </summary>
        /// <param name="viewName">The name of the view to validate</param>
        /// <param name="storedProcedureName">The name of the stored procedure to validate</param>
        /// <returns>A tuple with validation results for both objects</returns>
        public (bool viewExists, bool storedProcedureExists) ValidateDatabaseObjects(string viewName, string storedProcedureName)
        {
            return (ViewExists(viewName), StoredProcedureExists(storedProcedureName));
        }
    }
}