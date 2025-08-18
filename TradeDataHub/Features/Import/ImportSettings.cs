using System.Collections.Generic;
using TradeDataHub.Core.Models;

namespace TradeDataHub.Features.Import
{
    public class ImportSettingsRoot
    {
        public required ImportSettings ImportSettings { get; set; }
    }

    public class ImportSettings
    {
        public required ImportDatabaseSettings Database { get; set; }
        public required ImportFileSettings Files { get; set; }
        public required ImportLoggingSettings Logging { get; set; }
        public ImportObjectsSettings ImportObjects { get; set; }
    }

    public class ImportDatabaseSettings
    {
        public required string StoredProcedureName { get; set; }
        public required string ViewName { get; set; }
        public required string OrderByColumn { get; set; }
        public required string WorksheetName { get; set; }
    }

    public class ImportFileSettings
    {
        public required string OutputDirectory { get; set; }
        public required string FileSuffix { get; set; }
    }

    public class ImportLoggingSettings
    {
        public required string OperationLabel { get; set; }
        public required string LogFilePrefix { get; set; }
        public required string LogFileExtension { get; set; }
    }

    public class ImportObjectsSettings
    {
        public required string DefaultViewName { get; set; }
        public required string DefaultStoredProcedureName { get; set; }
        public List<DbObjectOption> Views { get; set; } = new List<DbObjectOption>();
        public List<DbObjectOption> StoredProcedures { get; set; } = new List<DbObjectOption>();
    }
}
