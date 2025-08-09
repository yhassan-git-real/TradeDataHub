using System.Collections.Generic;
using TradeDataHub.Core.Models;

namespace TradeDataHub.Features.Export
{
    public class ExportSettingsRoot
    {
        public required ExportSettings ExportSettings { get; set; }
    }

    public class ExportSettings
    {
        public required ExportOperationSettings Operation { get; set; }
        public required ExportFileSettings Files { get; set; }
        public required ExportLoggingSettings Logging { get; set; }
        public ExportObjectsSettings ExportObjects { get; set; }
    }

    public class ExportOperationSettings
    {
        public required string StoredProcedureName { get; set; }
        public required string ViewName { get; set; }
        public required string OrderByColumn { get; set; }
        public string WorksheetName { get; set; } = "Export Data";
    }

    public class ExportFileSettings
    {
        public required string OutputDirectory { get; set; }
    }

    public class ExportLoggingSettings
    {
        public string OperationLabel { get; set; } = "Excel Export Generation";
        public string LogFilePrefix { get; set; } = "ExportLog";
        public string LogFileExtension { get; set; } = ".txt";
    }

    public class ExportObjectsSettings
    {
        public string DefaultViewName { get; set; } = "EXPDATA";
        public string DefaultStoredProcedureName { get; set; } = "ExportData_New1";
        public List<DbObjectOption> Views { get; set; } = new List<DbObjectOption>();
        public List<DbObjectOption> StoredProcedures { get; set; } = new List<DbObjectOption>();
    }
}
