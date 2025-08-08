using System;
using System.Data;
using Microsoft.Data.SqlClient;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Features.Export;
using Microsoft.Extensions.Configuration;
using System.IO;
using TradeDataHub.Core.Database;

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

        public (SqlConnection connection, SqlDataReader reader, long recordCount) GetDataReader(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            SqlConnection? con = null;
            SqlDataReader? reader = null;
            try
            {
                con = new SqlConnection(_dbSettings.ConnectionString);
                con.Open();

                using (var cmd = new SqlCommand(_exportSettings.Operation.StoredProcedureName, con))
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

                long recordCount = 0;
                using (var countCmd = new SqlCommand($"SELECT COUNT(*) FROM {_exportSettings.Operation.ViewName}", con))
                {
                    recordCount = Convert.ToInt64(countCmd.ExecuteScalar());
                }

                var dataCmd = new SqlCommand($"SELECT * FROM {_exportSettings.Operation.ViewName} ORDER BY [{_exportSettings.Operation.OrderByColumn}]", con);
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
