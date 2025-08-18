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
using System.Threading.Tasks;
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
        private readonly StreamingExcelProcessor _streamingProcessor;
        private readonly ExcelObjectPoolManager _poolManager;
        
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

            // Initialize performance optimization components
            _streamingProcessor = new StreamingExcelProcessor(_logger);
            _poolManager = ExcelObjectPoolManager.Instance;
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

    public async Task<ImportExcelResult> CreateReportAsync(string fromMonth, string toMonth, string hsCode, string product,
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

                    // Use pooled ExcelPackage for better performance
                    var package = _poolManager.GetExcelPackage();
                    
                    try
                    {
                    var worksheet = package.Workbook.Worksheets.Add(_settings.Database.WorksheetName);

                    cancellationToken.ThrowIfCancellationRequested();

                    int fieldCount = reader.FieldCount;

                    cancellationToken.ThrowIfCancellationRequested();

                        // Add headers using optimized method
                        _streamingProcessor.AddHeaders(reader, worksheet);

                    cancellationToken.ThrowIfCancellationRequested();

                        // Load data using streaming processor for optimal memory usage
                        var streamingSuccess = _streamingProcessor.LoadDataFromReaderOptimized(reader, worksheet, recordCount, cancellationToken);
                        
                        if (!streamingSuccess)
                        {
                            throw new InvalidOperationException("Streaming data load failed");
                        }
                    
                    cancellationToken.ThrowIfCancellationRequested();

                    int lastRow = (int)recordCount + 1;
                    // Apply optimized range-based formatting
                    ApplyOptimizedFormatting(worksheet, lastRow, fieldCount);

                    cancellationToken.ThrowIfCancellationRequested();

                    Directory.CreateDirectory(_settings.Files.OutputDirectory);
                    var outputPath = Path.Combine(_settings.Files.OutputDirectory, fileName);
                    partialFilePath = outputPath; // Track for cleanup if cancelled

                    using var saveTimer = _logger.StartTimer("File Save", processId);
                        
                        // Use performance settings to determine optimal save strategy
                        var performanceSettings = ConfigurationCacheService.GetPerformanceSettings();
                        bool useMemoryStream = recordCount < performanceSettings.FileIO.MemoryStreamThreshold;
                        
                        if (useMemoryStream)
                        {
                            // Memory stream approach for smaller files
                        using var memoryStream = new MemoryStream();
                        package.SaveAs(memoryStream);
                            
                            if (performanceSettings.FileIO.AsyncFileOperations)
                            {
                                await File.WriteAllBytesAsync(outputPath, memoryStream.ToArray(), cancellationToken);
                    }
                    else
                    {
                                File.WriteAllBytes(outputPath, memoryStream.ToArray());
                            }
                        }
                        else
                        {
                            // Direct file save for larger datasets with optimized buffer
                            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, performanceSettings.FileIO.BufferSize);
                            package.SaveAs(fileStream);
                        }
                        
                    _logger.LogFileSave("Completed", saveTimer.Elapsed, processId);
                    saveTimer.Dispose();

                    partialFilePath = null; // File successfully created, don't clean up

                    _logger.LogExcelResult(fileName, excelTimer.Elapsed, recordCount, processId);
                    excelTimer.Dispose();

                    return new ImportExcelResult { Success = true, FileName = fileName, RowCount = recordCount };
                    }
                    finally
                    {
                        // Return package to pool for reuse
                        _poolManager.ReturnExcelPackage(package);
                    }
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

        /// <summary>
        /// Synchronous wrapper for backward compatibility - delegates to async implementation
        /// </summary>
        public ImportExcelResult CreateReport(string fromMonth, string toMonth, string hsCode, string product,
            string iec, string importer, string country, string name, string port, CancellationToken cancellationToken = default,
            string? viewName = null, string? storedProcedureName = null)
        {
            return CreateReportAsync(fromMonth, toMonth, hsCode, product, iec, importer, country, name, port, cancellationToken, viewName, storedProcedureName).GetAwaiter().GetResult();
        }

        private void ApplyOptimizedFormatting(OfficeOpenXml.ExcelWorksheet worksheet, int lastRow, int colCount)
        {
            if (lastRow <= 1) return;

            try
            {
                // Apply font settings using optimized pool manager
                var allCellsRange = worksheet.Cells[1, 1, lastRow, colCount];
                _poolManager.ApplyOptimizedRangeFormatting(allCellsRange, _format.FontName, _format.FontSize, _format.WrapText);

                // Apply column-specific formatting using pool manager
                _poolManager.ApplyColumnFormatting(worksheet, _format.DateColumns, _format.DateFormat, _format.TextColumns, colCount);

                // Apply header formatting using cached objects
                var headerRange = worksheet.Cells[1, 1, 1, colCount];
                _poolManager.ApplyHeaderFormatting(headerRange, _format.HeaderBackgroundColor, _format.BorderStyle);

                // Apply data formatting using cached border styles
                if (lastRow > 1)
                {
                    var dataRange = worksheet.Cells[2, 1, lastRow, colCount];
                    _poolManager.ApplyDataBorderFormatting(dataRange, _format.BorderStyle);
                }

                // Optimized AutoFit using deferred execution if enabled
                var performanceSettings = ConfigurationCacheService.GetPerformanceSettings();
                if (_format.AutoFitColumns && !performanceSettings.ExcelProcessing.DeferAutoFitColumns)
                {
                    _poolManager.PerformOptimizedAutoFit(worksheet, lastRow, colCount, _format.AutoFitSampleRows);
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
