using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using TradeDataHub.Core;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Config;
using OfficeOpenXml;              // EPPlus core
using OfficeOpenXml.Style;        // Styling
using System.Drawing;             // Color conversion

namespace TradeDataHub.Features.Export
{
    public enum SkipReason
    {
        None,
        NoData,
        ExcelRowLimit
    }

    public class ExcelResult
    {
        public bool Success { get; set; }
        public SkipReason SkipReason { get; set; }
        public string? FileName { get; set; }
        public int RowCount { get; set; }
    }

    public class ExcelService
    {
        private readonly ExcelFormatSettings _formatSettings;
        private readonly LoggingHelper _logger;

        public ExcelService()
        {
            _formatSettings = LoadExcelFormatSettings();
            _logger = LoggingHelper.Instance;
        }

        private ExcelFormatSettings LoadExcelFormatSettings()
        {
            const string jsonFileName = "Config/excelFormatting.json";
            
            if (!File.Exists(jsonFileName))
            {
                throw new FileNotFoundException($"Excel formatting file '{jsonFileName}' not found.");
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(jsonFileName, optional: false);
            
            var config = builder.Build();
            return config.Get<ExcelFormatSettings>()!;
        }

        public ExcelResult CreateReport(string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
        {
            var processId = _logger.GenerateProcessId();
            
            // Use centralized parameter management
            var parameterSet = ParameterHelper.CreateExportParameterSet(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
            var parameterKey = ParameterHelper.GenerateExportParameterKey(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
            var parameterDisplay = ParameterHelper.FormatParametersForDisplay(parameterSet);
            
            _logger.LogProcessStart("Excel Report Generation", parameterDisplay, processId);
            
            // Log detailed parameters for debugging
            _logger.LogDetailedParameters(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, processId);
            
            using var reportTimer = _logger.StartTimer("Total Process", processId);
            
            var dataAccess = new DataAccess();
            
            try
            {
                // Step 1: Execute stored procedure (SINGLE execution)
                _logger.LogStep("Database", "Executing stored procedure", processId);
                using var spTimer = _logger.StartTimer("Stored Procedure", processId);
                var (connection, reader, recordCount) = dataAccess.GetDataReader(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);

                // Config-driven names
                var configuredSpName = App.Settings.Database.StoredProcedureName;
                var configuredViewName = App.Settings.Database.ViewName;
                var configuredOrderColumn = App.Settings.Database.OrderByColumn;

                // Log SP execution details using centralized parameter formatting
                var spParams = ParameterHelper.FormatStoredProcedureParameters(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
                _logger.LogStoredProcedure(configuredSpName, spParams, spTimer.Elapsed, processId);
                spTimer.Dispose();

                try
                {
                    // Log row count validation
                    _logger.LogStep("Validation", $"Row count: {recordCount:N0}", processId);
                    _logger.LogDataReader(configuredViewName, configuredOrderColumn, recordCount, processId);

                    // Row count check
                    if (recordCount == 0)
                    {
                        _logger.LogProcessComplete("Excel Report Generation", reportTimer.Elapsed, "No data - skipped", processId);
                        return new ExcelResult 
                        { 
                            Success = false, 
                            SkipReason = SkipReason.NoData, 
                            RowCount = 0 
                        };
                    }

                    // Check Excel row limit (1,048,576 minus header row = 1,048,575 data rows)
                    if (recordCount > 1048575)
                    {
                        string skippedFileName = FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
                        _logger.LogSkipped(skippedFileName, recordCount, "Excel row limit exceeded", processId);
                        _logger.LogProcessComplete("Excel Report Generation", reportTimer.Elapsed, "Skipped - too many rows", processId);
                        return new ExcelResult 
                        { 
                            Success = false, 
                            SkipReason = SkipReason.ExcelRowLimit, 
                            FileName = skippedFileName, 
                            RowCount = (int)recordCount 
                        };
                    }

                    // Step 3: Dynamic Excel export (EPPlus)
                    string fileName = FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
                    _logger.LogExcelFileCreationStart(fileName, processId);
                    
                    using var excelTimer = _logger.StartTimer("Excel Creation", processId);
                    
                    // Create EPPlus package and worksheet
                    using var package = new ExcelPackage();
                    var worksheetName = App.Settings.Database.WorksheetName;
                    var worksheet = package.Workbook.Worksheets.Add(string.IsNullOrWhiteSpace(worksheetName) ? "Export Data" : worksheetName);

                    int fieldCount = reader.FieldCount;

                    // Set default styles (font, size, wrap) BEFORE loading data to reduce per-cell style churn
                    worksheet.Cells.Style.Font.Name = _formatSettings.FontName;
                    worksheet.Cells.Style.Font.Size = _formatSettings.FontSize;
                    worksheet.Cells.Style.WrapText = _formatSettings.WrapText;

                    // Pre-assign column-level number formats for date & text columns (applies to all rows, including those to be loaded)
                    foreach (int dateCol in _formatSettings.DateColumns)
                    {
                        if (dateCol > 0 && dateCol <= fieldCount)
                        {
                            worksheet.Column(dateCol).Style.Numberformat.Format = _formatSettings.DateFormat;
                        }
                    }
                    foreach (int textCol in _formatSettings.TextColumns)
                    {
                        if (textCol > 0 && textCol <= fieldCount)
                        {
                            worksheet.Column(textCol).Style.Numberformat.Format = "@";
                        }
                    }

                    // Write headers (no styling yet; styling deferred to single formatting pass)
                    for (int col = 0; col < fieldCount; col++)
                    {
                        worksheet.Cells[1, col + 1].Value = reader.GetName(col);
                    }

                    // Bulk load data rows starting at row 2
                    worksheet.Cells[2, 1].LoadFromDataReader(reader, false);

                    int lastRow = (int)recordCount + 1; // header row + data rows
                    ApplyExcelFormatting(worksheet, lastRow, fieldCount);

                    // Save file
                    string outputDir = App.Settings.Files.OutputDirectory;
                    Directory.CreateDirectory(outputDir);
                    string outputPath = Path.Combine(outputDir, fileName);

                    using var saveTimer = _logger.StartTimer("File Save", processId);
                    package.SaveAs(new FileInfo(outputPath));
                    _logger.LogFileSave("Completed", saveTimer.Elapsed, processId);
                    saveTimer.Dispose();
                    
                    _logger.LogExcelResult(fileName, excelTimer.Elapsed, recordCount, processId);
                    excelTimer.Dispose();
                    
                    _logger.LogProcessComplete("Excel Report Generation", reportTimer.Elapsed, $"Success - {fileName}", processId);
                    return new ExcelResult 
                    { 
                        Success = true, 
                        SkipReason = SkipReason.None, 
                        FileName = fileName, 
                        RowCount = (int)recordCount 
                    };
                }
                finally
                {
                    reader.Dispose();
                    connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Process failed: {ex.Message}", ex, processId);
                _logger.LogProcessComplete("Excel Report Generation", reportTimer.Elapsed, "Failed with error", processId);
                throw;
            }
        }

    private void ApplyExcelFormatting(ExcelWorksheet worksheet, int lastRow, int colCount)
        {
            if (lastRow <= 1) return; // only header present

            // Simplified: only honor "none" explicitly; otherwise always Thin as per requirement
            var borderStyle = (_formatSettings.BorderStyle?.Equals("none", StringComparison.OrdinalIgnoreCase) == true)
                ? ExcelBorderStyle.None
                : ExcelBorderStyle.Thin;

            // Header row styling
            var headerRange = worksheet.Cells[1, 1, 1, colCount];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(_formatSettings.HeaderBackgroundColor));
            headerRange.Style.Border.Top.Style = borderStyle;
            headerRange.Style.Border.Left.Style = borderStyle;
            headerRange.Style.Border.Right.Style = borderStyle;
            headerRange.Style.Border.Bottom.Style = borderStyle;

            // Data range styling (excluding header)
            if (lastRow > 1)
            {
                var dataRange = worksheet.Cells[2, 1, lastRow, colCount];
                // Borders per config
                dataRange.Style.Border.Top.Style = borderStyle;
                dataRange.Style.Border.Left.Style = borderStyle;
                dataRange.Style.Border.Right.Style = borderStyle;
                dataRange.Style.Border.Bottom.Style = borderStyle;
        // WrapText already applied globally pre-load; leave as-is
            }

            // Autofit columns if enabled
            if (_formatSettings.AutoFitColumns)
            {
                // Sample-based autofit: limit width calculation using configured sample row count (including header)
                int sampleLimit = _formatSettings.AutoFitSampleRows > 0 ? _formatSettings.AutoFitSampleRows : 1000;
                int sampleEndRow = Math.Min(lastRow, sampleLimit);
        worksheet.Cells[1, 1, sampleEndRow, colCount].AutoFitColumns();
            }
        }

        // Retained utility (may be used elsewhere or for future range building)
        private string GetColumnLetter(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }
            return columnName;
        }
    }
}
