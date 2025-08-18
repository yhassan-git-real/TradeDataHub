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
        public required string WorksheetName { get; set; }
    }

    public class ExportFileSettings
    {
        public required string OutputDirectory { get; set; }
    }

    public class ExportLoggingSettings
    {
        public required string OperationLabel { get; set; }
        public required string LogFilePrefix { get; set; }
        public required string LogFileExtension { get; set; }
    }

    public class ExportObjectsSettings
    {
        public required string DefaultViewName { get; set; }
        public required string DefaultStoredProcedureName { get; set; }
        public List<DbObjectOption> Views { get; set; } = new List<DbObjectOption>();
        public List<DbObjectOption> StoredProcedures { get; set; } = new List<DbObjectOption>();
    }
}
