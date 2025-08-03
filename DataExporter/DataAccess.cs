using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace TradeDataHub
{
    public class DataAccess
    {
        /// <summary>
        /// Test the database connection
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                var connectionString = App.Settings.Database.ConnectionString;
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection test failed: {ex.Message}", "Connection Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        /// <summary>
        /// This method connects to the SQL Server database and executes a stored procedure
        /// to fetch trade data based on the provided filter criteria.
        /// </summary>
        public DataTable GetData(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            var dt = new DataTable("ExportData");
            try
            {
                var connectionString = App.Settings.Database.ConnectionString;
                var storedProcName = App.Settings.Database.StoredProcedureName;

                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // First execute the stored procedure
                    using (var cmd = new SqlCommand(storedProcName, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 300; // 5 minutes timeout

                        // Add parameters to the command (matching stored procedure parameters)
                        cmd.Parameters.AddWithValue("@fromMonth", int.Parse(fromMonth));
                        cmd.Parameters.AddWithValue("@ToMonth", int.Parse(toMonth));
                        cmd.Parameters.AddWithValue("@hs", hsCode);
                        cmd.Parameters.AddWithValue("@prod", product);
                        cmd.Parameters.AddWithValue("@Iec", iec);
                        cmd.Parameters.AddWithValue("@ExpCmp", exporter);
                        cmd.Parameters.AddWithValue("@forcount", country);
                        cmd.Parameters.AddWithValue("@forname", name);
                        cmd.Parameters.AddWithValue("@port", port);

                        cmd.ExecuteNonQuery();
                    }
                    
                    // Then query the configured view to get the results
                    var viewName = App.Settings.Database.ViewName;
                    var orderByColumn = App.Settings.Database.OrderByColumn;
                    using (var cmd = new SqlCommand($"SELECT * FROM {viewName} ORDER BY [{orderByColumn}]", con))
                    {
                        using (var da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log the exception or show a more specific error message
                string errorMessage = ex.Number switch
                {
                    2 => "Cannot connect to SQL Server. Please check if the server is running and accessible.",
                    18456 => "Login failed. Please check your username and password.",
                    4060 => "Cannot open database. Please check if the database exists.",
                    -2 => "Connection timeout. Please check your network connection.",
                    _ => $"A database error occurred: {ex.Message}"
                };
                MessageBox.Show(errorMessage, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Catch other potential exceptions
                MessageBox.Show($"An unexpected error occurred during data access: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return dt;
        }
    }
}
