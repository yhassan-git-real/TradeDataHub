using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Core.Services;
using System.Threading;
using TradeDataHub.Core.Cancellation;

namespace TradeDataHub.Features.Import
{
    public class ImportExcelResult
    {
        public bool Success { get; set; }
        public string? FileName { get; set; }
        public long RowCount { get; set; }
        public string? SkipReason { get; set; }
        public bool IsCancelled => SkipReason == "Cancelled";
    }

    public class ImportExcelService
    {
        private readonly ModuleLogger _logger;
        private readonly ImportSettings _settings;
        private readonly ImportExcelFormatSettings _format;
        
        // Public property to access import settings
        public ImportSettings ImportSettings => _settings;

        public ImportExcelService()
        {
            _logger = ModuleLoggerFactory.GetImportLogger();
            // Use cached configuration loading for better performance
            _settings = ConfigurationCacheService.GetImportSettings();
            
            try
            {
                _format = ConfigurationCacheService.GetImportExcelFormatSettings();
                if (_format == null)
                {
                    throw new InvalidOperationException("ImportExcelFormatSettings loaded as null but is required");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load ImportExcelFormatSettings: {ex.Message}", ex, "INIT");
                throw new InvalidOperationException("ImportExcelFormatSettings is required but failed to load", ex);
            }
        }

        private ImportSettings LoadImportSettings()
        {
            const string json = "Config/import.appsettings.json";
            if (!File.Exists(json)) throw new FileNotFoundException($"Missing import settings file: {json}");
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json, false);
            var cfg = builder.Build();
            var root = cfg.Get<ImportSettingsRoot>();
            if (root == null) throw new InvalidOperationException("Failed to bind ImportSettingsRoot");
            return root.ImportSettings;
        }

        private ImportExcelFormatSettings LoadImportFormatting()
        {
            const string json = "Config/ImportExcelFormatSettings.json";
            if (!File.Exists(json)) throw new FileNotFoundException($"Missing import formatting file: {json}");
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json, false);
            var cfg = builder.Build();
            return cfg.Get<ImportExcelFormatSettings>()!;
        }

    public ImportExcelResult CreateReport(string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port, CancellationToken cancellationToken = default,
            string? viewName = null, string? storedProcedureName = null)
        {
            var processId = _logger.GenerateProcessId();
            _logger.LogProcessStart(_settings.Logging.OperationLabel, $"fromMonth:{fromMonth}, toMonth:{toMonth}, hsCode:{hsCode}, product:{product}, iec:{iec}, importer:{importer}, country:{country}, name:{name}, port:{port}", processId);
            using var totalTimer = _logger.StartTimer("Total Process", processId);
            string? partialFilePath = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataAccess = new ImportDataAccess(_settings);
                
                // Use provided view and stored procedure names if specified, otherwise use defaults
                string effectiveViewName = viewName ?? _settings.Database.ViewName;
                string effectiveStoredProcedureName = storedProcedureName ?? _settings.Database.StoredProcedureName;
                _logger.LogStep("Database", "Executing stored procedure", processId);
                using var spTimer = _logger.StartTimer("Stored Procedure", processId);
                var (connection, reader, recordCount) = dataAccess.GetDataReader(fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, cancellationToken, effectiveViewName, effectiveStoredProcedureName);
                spTimer.Dispose();

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogStep("Validation", $"Row count: {recordCount:N0}", processId);

                    if (recordCount == 0)
                    {
                        ModuleSkippedDatasetLogger.LogImportSkippedDataset(0, 0, fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, "NoData");
                        _logger.LogProcessComplete(_settings.Logging.OperationLabel, totalTimer.Elapsed, "No data - skipped", processId);
                        return new ImportExcelResult { Success = false, RowCount = 0, SkipReason = "NoData" };
                    }
                    if (recordCount > ImportParameterHelper.MAX_EXCEL_ROWS)
                    {
                        ModuleSkippedDatasetLogger.LogImportSkippedDataset(0, recordCount, fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, "ExcelRowLimit");
                        _logger.LogProcessComplete(_settings.Logging.OperationLabel, totalTimer.Elapsed, "Row limit - skipped", processId);
                        return new ImportExcelResult { Success = false, RowCount = recordCount, SkipReason = "ExcelRowLimit" };
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    string fileName = TradeDataHub.Core.Helpers.Import_FileNameHelper.GenerateImportFileName(fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, _settings.Files.FileSuffix);
                    _logger.LogExcelFileCreationStart(fileName, processId);
                    using var excelTimer = _logger.StartTimer("Excel Creation", processId);

                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add(_settings.Database.WorksheetName);

                    cancellationToken.ThrowIfCancellationRequested();

                    int fieldCount = reader.FieldCount;

                    cancellationToken.ThrowIfCancellationRequested();

                    // Add headers first
                    for (int col = 0; col < fieldCount; col++)
                        worksheet.Cells[1, col + 1].Value = reader.GetName(col);

                    cancellationToken.ThrowIfCancellationRequested();

                    // Load data from reader
                    worksheet.Cells[2, 1].LoadFromDataReader(reader, false);
                    
                    cancellationToken.ThrowIfCancellationRequested();

                    int lastRow = (int)recordCount + 1;
                    // Apply optimized range-based formatting
                    ApplyOptimizedFormatting(worksheet, lastRow, fieldCount);

                    cancellationToken.ThrowIfCancellationRequested();

                    Directory.CreateDirectory(_settings.Files.OutputDirectory);
                    var outputPath = Path.Combine(_settings.Files.OutputDirectory, fileName);
                    partialFilePath = outputPath; // Track for cleanup if cancelled

                    using var saveTimer = _logger.StartTimer("File Save", processId);
                    // Use memory stream for better performance with smaller files
                    if (recordCount < 50000) // Use memory stream for smaller datasets
                    {
                        using var memoryStream = new MemoryStream();
                        package.SaveAs(memoryStream);
                        File.WriteAllBytes(outputPath, memoryStream.ToArray());
                    }
                    else
                    {
                        // For larger datasets, use direct file save
                        package.SaveAs(new FileInfo(outputPath));
                    }
                    _logger.LogFileSave("Completed", saveTimer.Elapsed, processId);
                    saveTimer.Dispose();

                    partialFilePath = null; // File successfully created, don't clean up

                    _logger.LogExcelResult(fileName, excelTimer.Elapsed, recordCount, processId);
                    excelTimer.Dispose();
                    _logger.LogProcessComplete(_settings.Logging.OperationLabel, totalTimer.Elapsed, $"Success - {fileName}", processId);

                    return new ImportExcelResult { Success = true, FileName = fileName, RowCount = recordCount };
                }
                finally
                {
                    reader.Dispose();
                    connection.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogProcessComplete(_settings.Logging.OperationLabel, totalTimer.Elapsed, "Cancelled by user", processId);
                
                // Clean up partial file if it exists
                CancellationCleanupHelper.SafeDeletePartialFile(partialFilePath, processId);
                
                return new ImportExcelResult { Success = false, RowCount = 0, SkipReason = "Cancelled" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Import process failed: {ex.Message}", ex, processId);
                _logger.LogProcessComplete(_settings.Logging.OperationLabel, totalTimer.Elapsed, "Failed with error", processId);
                throw;
            }
        }

        private void ApplyOptimizedFormatting(OfficeOpenXml.ExcelWorksheet worksheet, int lastRow, int colCount)
        {
            if (lastRow <= 1) return;

            try
            {
                // Apply font settings to entire worksheet range at once (much faster than cell-by-cell)
                var allCellsRange = worksheet.Cells[1, 1, lastRow, colCount];
                allCellsRange.Style.Font.Name = _format.FontName;
                allCellsRange.Style.Font.Size = _format.FontSize;
                allCellsRange.Style.WrapText = _format.WrapText;

                // Apply column-specific formatting in batches
                if (_format.DateColumns != null && _format.DateColumns.Length > 0)
                {
                    foreach (int dateCol in _format.DateColumns)
                    {
                        if (dateCol > 0 && dateCol <= colCount)
                            worksheet.Column(dateCol).Style.Numberformat.Format = _format.DateFormat;
                    }
                }
                
                if (_format.TextColumns != null && _format.TextColumns.Length > 0)
                {
                    foreach (int textCol in _format.TextColumns)
                    {
                        if (textCol > 0 && textCol <= colCount)
                            worksheet.Column(textCol).Style.Numberformat.Format = "@";
                    }
                }

                var borderStyle = (_format.BorderStyle?.Equals("none", StringComparison.OrdinalIgnoreCase) == true)
                    ? ExcelBorderStyle.None : ExcelBorderStyle.Thin;

                // Apply header formatting to entire header row at once
                var headerRange = worksheet.Cells[1,1,1,colCount];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                
                // Parse header color from configuration
                var headerColor = ColorTranslator.FromHtml(_format.HeaderBackgroundColor);
                headerRange.Style.Fill.BackgroundColor.SetColor(headerColor);
                
                headerRange.Style.Border.Top.Style = borderStyle;
                headerRange.Style.Border.Left.Style = borderStyle;
                headerRange.Style.Border.Right.Style = borderStyle;
                headerRange.Style.Border.Bottom.Style = borderStyle;

                // Apply data formatting to entire data range at once
                if (lastRow > 1)
                {
                    var dataRange = worksheet.Cells[2,1,lastRow,colCount];
                    dataRange.Style.Border.Top.Style = borderStyle;
                    dataRange.Style.Border.Left.Style = borderStyle;
                    dataRange.Style.Border.Right.Style = borderStyle;
                    dataRange.Style.Border.Bottom.Style = borderStyle;
                }

                // Auto-fit columns using sample data for performance
                if (_format.AutoFitColumns)
                {
                    int sampleEndRow = Math.Min(lastRow, _format.AutoFitSampleRows);
                    worksheet.Cells[1,1,sampleEndRow,colCount].AutoFitColumns();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying formatting from JSON configuration: {ex.Message}", ex, "FORMAT");
                throw; // Re-throw the exception since we no longer have fallback formatting
            }
        }


    }
}
