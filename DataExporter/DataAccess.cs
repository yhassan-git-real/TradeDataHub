using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;

namespace TradeDataHub
{
    public class DataAccess
    {
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
                    using (var cmd = new SqlCommand(storedProcName, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters to the command
                        cmd.Parameters.AddWithValue("@frommonth", fromMonth);
                        cmd.Parameters.AddWithValue("@tomonth", toMonth);
                        cmd.Parameters.AddWithValue("@Hscode", hsCode);
                        cmd.Parameters.AddWithValue("@product", product);
                        cmd.Parameters.AddWithValue("@iec", iec);
                        cmd.Parameters.AddWithValue("@exporter", exporter);
                        cmd.Parameters.AddWithValue("@forcountry", country);
                        cmd.Parameters.AddWithValue("@forname", name);
                        cmd.Parameters.AddWithValue("@port", port);

                        con.Open();

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
                MessageBox.Show($"A database error occurred: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
