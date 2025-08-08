using System;
using System.Data;
using Microsoft.Data.SqlClient;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Core.Database;
using Microsoft.Extensions.Configuration;
using System.IO;

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
            string fromMonth, string toMonth, string hsCode, string product, string iec, string importer, string country, string name, string port)
        {
            SqlConnection? con = null;
            SqlDataReader? reader = null;

            try
            {
                con = new SqlConnection(_dbSettings.ConnectionString);
                con.Open();

                // Execute stored procedure once
                using (var cmd = new SqlCommand(_settings.Database.StoredProcedureName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 50000;

                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_FROM_MONTH, int.Parse(fromMonth));
                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_TO_MONTH, int.Parse(toMonth));
                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_HS_CODE, hsCode);
                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_PRODUCT, product);
                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_IEC, iec);
                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_IMPORTER, importer);
                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_FOREIGN_COUNTRY, country);
                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_FOREIGN_NAME, name);
                    cmd.Parameters.AddWithValue(Core.Helpers.Import_ParameterHelper.StoredProcedureParameters.SP_PORT, port);

                    cmd.ExecuteNonQuery();
                }

                // Row count
                long recordCount = 0;
                using (var countCmd = new SqlCommand($"SELECT COUNT(*) FROM {_settings.Database.ViewName}", con))
                {
                    recordCount = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                // Open streaming reader
                var dataCmd = new SqlCommand($"SELECT * FROM {_settings.Database.ViewName} ORDER BY [{_settings.Database.OrderByColumn}]", con);
                dataCmd.CommandTimeout = 50000;
                reader = dataCmd.ExecuteReader();

                return (con, reader, recordCount);
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
