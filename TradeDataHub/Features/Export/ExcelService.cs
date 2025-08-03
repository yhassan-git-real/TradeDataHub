using ClosedXML.Excel;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using TradeDataHub.Core;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Config;

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
                
                // Log SP execution details using centralized parameter formatting
                var spParams = ParameterHelper.FormatStoredProcedureParameters(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
                _logger.LogStoredProcedure("ExportData_New1", spParams, spTimer.Elapsed, processId);
                spTimer.Dispose();

                try
                {
                    // Log row count validation
                    _logger.LogStep("Validation", $"Row count: {recordCount:N0}", processId);
                    _logger.LogDataReader("EXPDATA", "sb_DATE", recordCount, processId);

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

                    // Step 3: Dynamic Excel export
                    string fileName = FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
                    _logger.LogExcelFileCreationStart(fileName, processId);
                    
                    using var excelTimer = _logger.StartTimer("Excel Creation", processId);
                    
                    // Create new workbook dynamically (no template dependency)
                    var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Export Data");

                    // Step 3A: Dynamic headers from SQL view
                    WriteColumnHeaders(worksheet, reader);

                    // Step 3B: Load data starting from row 2
                    int lastRow = WriteDataToWorksheet(worksheet, reader);

                    // Apply Excel formatting
                    ApplyExcelFormatting(worksheet, lastRow, reader.FieldCount);

                    // Save file
                    string outputDir = App.Settings.Files.OutputDirectory;
                    Directory.CreateDirectory(outputDir);
                    string outputPath = Path.Combine(outputDir, fileName);
                    
                    using var saveTimer = _logger.StartTimer("File Save", processId);
                    workbook.SaveAs(outputPath);
                    workbook.Dispose();
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
        }        private void WriteColumnHeaders(IXLWorksheet worksheet, SqlDataReader reader)
        {
            // Write column names from SqlDataReader to row 1
            for (int col = 0; col < reader.FieldCount; col++)
            {
                var headerCell = worksheet.Cell(1, col + 1);
                headerCell.Value = reader.GetName(col);
                
                // Apply header formatting
                headerCell.Style.Font.FontName = _formatSettings.FontName;
                headerCell.Style.Font.FontSize = _formatSettings.FontSize;
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml(_formatSettings.HeaderBackgroundColor);
                headerCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }        private int WriteDataToWorksheet(IXLWorksheet worksheet, SqlDataReader reader)
        {
            int row = 2; // Start at row 2 (headers are in row 1)
            int colCount = reader.FieldCount;
            
            // Write data starting from A2
            while (reader.Read())
            {
                for (int col = 0; col < colCount; col++)
                {
                    var cell = worksheet.Cell(row, col + 1);
                    var value = reader.GetValue(col);
                    
                    if (reader.IsDBNull(col))
                    {
                        cell.Value = "";
                    }
                    else if (value is DateTime dateValue)
                    {
                        // Keep DateTime as DateTime for proper Excel formatting
                        cell.Value = dateValue;
                    }
                    else if (_formatSettings.TextColumns.Contains(col + 1))
                    {
                        // Format as text to preserve leading zeros
                        cell.Style.NumberFormat.Format = "@";
                        cell.Value = value.ToString();
                    }
                    else
                    {
                        cell.Value = value.ToString();
                    }
                }
                row++;
            }
            
            return row - 1;
        }

        private void ApplyExcelFormatting(IXLWorksheet worksheet, int lastRow, int colCount)
        {
            if (lastRow <= 1) return;

            var dataRange = worksheet.Range($"A2:{GetColumnLetter(colCount)}{lastRow}");
            
            dataRange.Style.Font.FontName = _formatSettings.FontName;
            dataRange.Style.Font.FontSize = _formatSettings.FontSize;
            
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            
            foreach (int dateCol in _formatSettings.DateColumns)
            {
                if (dateCol <= colCount)
                {
                    var dateRange = worksheet.Range($"{GetColumnLetter(dateCol)}2:{GetColumnLetter(dateCol)}{lastRow}");
                    dateRange.Style.NumberFormat.Format = _formatSettings.DateFormat;
                }
            }
            
            foreach (int textCol in _formatSettings.TextColumns)
            {
                if (textCol <= colCount)
                {
                    var textRange = worksheet.Range($"{GetColumnLetter(textCol)}2:{GetColumnLetter(textCol)}{lastRow}");
                    textRange.Style.NumberFormat.Format = "@"; // Store as text
                }
            }
            
            if (_formatSettings.AutoFitColumns)
            {
                worksheet.Columns().AdjustToContents();
            }
            
            dataRange.Style.Alignment.WrapText = _formatSettings.WrapText;
            
            worksheet.Cell("A1").SetActive();
        }

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
