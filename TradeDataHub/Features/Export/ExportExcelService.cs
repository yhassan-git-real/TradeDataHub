using System;
using System.Data;
using System.IO;
using System.Drawing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TradeDataHub.Config;
using TradeDataHub.Core.DataAccess;
using TradeDataHub.Core.Database;
using TradeDataHub.Core.Helpers;
using TradeDataHub.Core.Logging;
using TradeDataHub.Core.Services;
using System.Threading;
using System.Threading.Tasks;
using TradeDataHub.Core.Cancellation;

namespace TradeDataHub.Features.Export;

public enum SkipReason
{
	None,
	NoData,
	ExcelRowLimit,
	Cancelled
}

public class ExcelResult
{
	public bool Success { get; set; }
	public SkipReason SkipReason { get; set; }
	public string? FileName { get; set; }
	public int RowCount { get; set; }
	public bool IsCancelled => SkipReason == SkipReason.Cancelled;
}

public class ExportExcelService
{
	private readonly ExcelFormatSettings _formatSettings;
	private readonly ModuleLogger _logger;
	private readonly ExportSettings _exportSettings;
	private readonly SharedDatabaseSettings _dbSettings;
	private readonly StreamingExcelProcessor _streamingProcessor;
	private readonly ExcelObjectPoolManager _poolManager;
	
	// Public property to access export settings
	public ExportSettings ExportSettings => _exportSettings;

	public ExportExcelService()
	{
		// Use cached configuration loading for better performance
		_formatSettings = ConfigurationCacheService.GetExcelFormatSettings();
		_logger = ModuleLoggerFactory.GetExportLogger();
		_exportSettings = ConfigurationCacheService.GetExportSettings();
		_dbSettings = ConfigurationCacheService.GetSharedDatabaseSettings();
		
		// Initialize performance optimization components
		_streamingProcessor = new StreamingExcelProcessor(_logger);
		_poolManager = ExcelObjectPoolManager.Instance;
	}

	public async Task<ExcelResult> CreateReportAsync(int combinationNumber, string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port, CancellationToken cancellationToken = default, string? viewName = null, string? storedProcedureName = null)
	{
		var processId = _logger.GenerateProcessId();

		var parameterSet = ExportParameterHelper.CreateExportParameterSet(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
		var parameterDisplay = ExportParameterHelper.FormatParametersForDisplay(parameterSet);
		_logger.LogProcessStart("Excel Export Generation", parameterDisplay, processId);

		using var reportTimer = _logger.StartTimer("Total Process", processId);
		var dataAccess = new ExportDataAccess();
			// Use provided view and stored procedure names if specified, otherwise use defaults
			string effectiveViewName = viewName ?? _exportSettings.Operation.ViewName;
			string effectiveStoredProcedureName = storedProcedureName ?? _exportSettings.Operation.StoredProcedureName;
		string? partialFilePath = null;

		try
		{
			cancellationToken.ThrowIfCancellationRequested();

			_logger.LogStep("Database", "Executing stored procedure", processId);
			using var spTimer = _logger.StartTimer("Stored Procedure", processId);
			var (connection, reader, recordCount) = dataAccess.GetDataReader(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, cancellationToken, effectiveViewName, effectiveStoredProcedureName);
			spTimer.Dispose();

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				_logger.LogStep("Validation", $"Row count: {recordCount:N0}", processId);
				if (recordCount == 0)
				{
					ModuleSkippedDatasetLogger.LogExportSkippedDataset(combinationNumber, 0, fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, "NoData");
					_logger.LogProcessComplete("Excel Export Generation", reportTimer.Elapsed, "No data - skipped", processId);
					return new ExcelResult { Success = false, SkipReason = SkipReason.NoData, RowCount = 0 };
				}
				if (recordCount > ExportParameterHelper.MAX_EXCEL_ROWS)
				{
					string skippedFileName = Export_FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
					ModuleSkippedDatasetLogger.LogExportSkippedDataset(combinationNumber, recordCount, fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, "RowLimit");
					_logger.LogSkipped(skippedFileName, recordCount, "Excel row limit exceeded", processId);
					_logger.LogProcessComplete("Excel Export Generation", reportTimer.Elapsed, "Skipped - too many rows", processId);
					return new ExcelResult { Success = false, SkipReason = SkipReason.ExcelRowLimit, FileName = skippedFileName, RowCount = (int)recordCount };
				}

				cancellationToken.ThrowIfCancellationRequested();

					string fileName = Export_FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
				_logger.LogExcelFileCreationStart(fileName, processId);
				using var excelTimer = _logger.StartTimer("Excel Creation", processId);
				
				// Use pooled ExcelPackage for better performance
				var package = _poolManager.GetExcelPackage();
				
				try
				{
					var worksheetName = _exportSettings.Operation.WorksheetName;
					var worksheet = package.Workbook.Worksheets.Add(worksheetName);

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
					
					int lastRow = (int)recordCount + 1; // include header
					
					// Apply optimized formatting using pool manager
					ApplyOptimizedExcelFormatting(worksheet, lastRow, fieldCount);
				
				cancellationToken.ThrowIfCancellationRequested();

				string outputDir = _exportSettings.Files.OutputDirectory;
				Directory.CreateDirectory(outputDir);
				string outputPath = Path.Combine(outputDir, fileName);
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
					
					return new ExcelResult { Success = true, SkipReason = SkipReason.None, FileName = fileName, RowCount = (int)recordCount };
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
			_logger.LogProcessComplete("Excel Export Generation", reportTimer.Elapsed, "Cancelled by user", processId);
			
			// Clean up partial file if it exists
			CancellationCleanupHelper.SafeDeletePartialFile(partialFilePath, processId);
			
			return new ExcelResult { Success = false, SkipReason = SkipReason.Cancelled, RowCount = 0 };
		}
		catch (Exception ex)
		{
			_logger.LogError($"Process failed: {ex.Message}", ex, processId);
			_logger.LogProcessComplete("Excel Export Generation", reportTimer.Elapsed, "Failed with error", processId);
			throw;
		}
	}

	/// <summary>
	/// Synchronous wrapper for backward compatibility - delegates to async implementation
	/// </summary>
	public ExcelResult CreateReport(int combinationNumber, string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port, CancellationToken cancellationToken = default, string? viewName = null, string? storedProcedureName = null)
	{
		return CreateReportAsync(combinationNumber, fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, cancellationToken, viewName, storedProcedureName).GetAwaiter().GetResult();
	}

	private void ApplyOptimizedExcelFormatting(ExcelWorksheet worksheet, int lastRow, int colCount)
	{
		if (lastRow <= 1) return;

		try
		{
			// Apply font settings using optimized pool manager
			var allCellsRange = worksheet.Cells[1, 1, lastRow, colCount];
			_poolManager.ApplyOptimizedRangeFormatting(allCellsRange, _formatSettings.FontName, _formatSettings.FontSize, _formatSettings.WrapText);

			// Apply column-specific formatting using pool manager
			_poolManager.ApplyColumnFormatting(worksheet, _formatSettings.DateColumns, _formatSettings.DateFormat, _formatSettings.TextColumns, colCount);

			// Apply header formatting using cached objects
			var headerRange = worksheet.Cells[1, 1, 1, colCount];
			_poolManager.ApplyHeaderFormatting(headerRange, _formatSettings.HeaderBackgroundColor, _formatSettings.BorderStyle);

			// Apply data formatting using cached border styles
			if (lastRow > 1)
			{
				var dataRange = worksheet.Cells[2, 1, lastRow, colCount];
				_poolManager.ApplyDataBorderFormatting(dataRange, _formatSettings.BorderStyle);
			}

			// Optimized AutoFit using deferred execution if enabled
			var performanceSettings = ConfigurationCacheService.GetPerformanceSettings();
			if (_formatSettings.AutoFitColumns && !performanceSettings.ExcelProcessing.DeferAutoFitColumns)
			{
				_poolManager.PerformOptimizedAutoFit(worksheet, lastRow, colCount, _formatSettings.AutoFitSampleRows);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error applying formatting from JSON configuration: {ex.Message}", ex, "FORMAT");
			throw; // Re-throw the exception since we no longer have fallback formatting
		}
	}

	private string GetColumnLetter(int columnNumber)
	{
		string columnName = string.Empty;
		while (columnNumber > 0)
		{
			int modulo = (columnNumber - 1) % 26;
			columnName = Convert.ToChar('A' + modulo) + columnName;
			columnNumber = (columnNumber - modulo) / 26;
		}
		return columnName;
	}
}
