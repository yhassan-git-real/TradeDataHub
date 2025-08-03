using System.Data;

namespace TradeDataHub
{
    public class DataAccess
    {
        /// <summary>
        /// MOCK IMPLEMENTATION FOR TESTING.
        /// This method simulates fetching data from the database.
        /// In the real implementation, this would connect to SQL Server and run the stored procedure.
        /// </summary>
        public DataTable GetData(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            // In a real implementation, you would use the settings like this:
            // var connectionString = App.Settings.Database.ConnectionString;
            // var storedProcName = App.Settings.Database.StoredProcedureName;
            // using (var con = new SqlConnection(connectionString)) { ... }

            System.Diagnostics.Debug.WriteLine($"Simulating call to stored procedure: {App.Settings.Database.StoredProcedureName}");

            var dt = new DataTable("ExportData");
            dt.Columns.Add("sb_no", typeof(string));
            dt.Columns.Add("sb_date", typeof(System.DateTime));
            dt.Columns.Add("port_of_origin", typeof(string));
            dt.Columns.Add("hs_code", typeof(string));
            dt.Columns.Add("product", typeof(string));
            dt.Columns.Add("quantity", typeof(decimal));
            dt.Columns.Add("value_usd", typeof(decimal));
            dt.Columns.Add("exporter_name", typeof(string));

            dt.Rows.Add($"SB{new System.Random().Next(1000, 9999)}", System.DateTime.Now.AddDays(-10), port, hsCode, product, 150.5m, 30100.75m, exporter);
            dt.Rows.Add($"SB{new System.Random().Next(1000, 9999)}", System.DateTime.Now.AddDays(-5), port, hsCode, product, 200.0m, 45200.00m, exporter);
            dt.Rows.Add($"SB{new System.Random().Next(1000, 9999)}", System.DateTime.Now, port, hsCode, product, 120.75m, 28000.50m, exporter);

            if (product.Contains("test_no_data", System.StringComparison.OrdinalIgnoreCase))
            {
                dt.Clear();
            }

            return dt;
        }
    }
}
