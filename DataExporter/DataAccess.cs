using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Windows;

namespace TradeDataHub
{
    public class DataAccess
    {
        public async Task<DataTable> GetDataAsync(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            var connectionString = App.Settings.Database.ConnectionString;
            var storedProcName = App.Settings.Database.StoredProcedureName;
            var dt = new DataTable();

            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    // Step 1: Execute the stored procedure to populate the data table
                    using (var cmd = new SqlCommand(storedProcName, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 500; // 500 seconds, similar to legacy

                        // Add parameters securely
                        cmd.Parameters.AddWithValue("@FromMonth", fromMonth);
                        cmd.Parameters.AddWithValue("@ToMonth", toMonth);
                        cmd.Parameters.AddWithValue("@HSCode", hsCode);
                        cmd.Parameters.AddWithValue("@Product", product);
                        cmd.Parameters.AddWithValue("@IEC", iec);
                        cmd.Parameters.AddWithValue("@Exporter", exporter);
                        cmd.Parameters.AddWithValue("@ForCount", country);
                        cmd.Parameters.AddWithValue("@ForName", name);
                        cmd.Parameters.AddWithValue("@Port", port);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Step 2: Select the data from the now-populated table
                    using (var cmd = new SqlCommand("SELECT * FROM EXPDATA ORDER BY [sb_DATE]", con))
                    {
                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Handle potential SQL errors gracefully
                MessageBox.Show($"Database error: {ex.Message}", "SQL Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null; // Return null to indicate failure
            }

            return dt;
        }
    }
}
