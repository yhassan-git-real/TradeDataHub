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
	
	// Public property to access export settings
	public ExportSettings ExportSettings => _exportSettings;

	public ExportExcelService()
	{
		// Use cached configuration loading for better performance
		_formatSettings = ConfigurationCacheService.GetExcelFormatSettings();
		_logger = ModuleLoggerFactory.GetExportLogger();
		_exportSettings = ConfigurationCacheService.GetExportSettings();
		_dbSettings = ConfigurationCacheService.GetSharedDatabaseSettings();
	}

	public ExcelResult CreateReport(int combinationNumber, string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port, CancellationToken cancellationToken = default, string? viewName = null, string? storedProcedureName = null)
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
				using var package = new ExcelPackage();
				var worksheetName = _exportSettings.Operation.WorksheetName;
				var worksheet = package.Workbook.Worksheets.Add(worksheetName);

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
				
				int lastRow = (int)recordCount + 1; // include header
				
				// Apply range-based formatting for better performance
				ApplyOptimizedExcelFormatting(worksheet, lastRow, fieldCount);
				
				cancellationToken.ThrowIfCancellationRequested();

				string outputDir = _exportSettings.Files.OutputDirectory;
				Directory.CreateDirectory(outputDir);
				string outputPath = Path.Combine(outputDir, fileName);
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
				_logger.LogProcessComplete("Excel Export Generation", reportTimer.Elapsed, $"Success - {fileName}", processId);
				return new ExcelResult { Success = true, SkipReason = SkipReason.None, FileName = fileName, RowCount = (int)recordCount };
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

	private void ApplyOptimizedExcelFormatting(ExcelWorksheet worksheet, int lastRow, int colCount)
	{
		if (lastRow <= 1) return;

		try
		{
			// Apply font settings to entire worksheet range at once (much faster than cell-by-cell)
			var allCellsRange = worksheet.Cells[1, 1, lastRow, colCount];
			allCellsRange.Style.Font.Name = _formatSettings.FontName;
			allCellsRange.Style.Font.Size = _formatSettings.FontSize;
			allCellsRange.Style.WrapText = _formatSettings.WrapText;

			// Apply column-specific formatting in batches
			foreach (int dateCol in _formatSettings.DateColumns)
				if (dateCol > 0 && dateCol <= colCount)
					worksheet.Column(dateCol).Style.Numberformat.Format = _formatSettings.DateFormat;
					
			foreach (int textCol in _formatSettings.TextColumns)
				if (textCol > 0 && textCol <= colCount)
					worksheet.Column(textCol).Style.Numberformat.Format = "@";

			var borderStyle = (_formatSettings.BorderStyle?.Equals("none", StringComparison.OrdinalIgnoreCase) == true)
				? ExcelBorderStyle.None : ExcelBorderStyle.Thin;

			// Apply header formatting to entire header row at once
			var headerRange = worksheet.Cells[1, 1, 1, colCount];
			headerRange.Style.Font.Bold = true;
			headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
			headerRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(_formatSettings.HeaderBackgroundColor));
			headerRange.Style.Border.Top.Style = borderStyle;
			headerRange.Style.Border.Left.Style = borderStyle;
			headerRange.Style.Border.Right.Style = borderStyle;
			headerRange.Style.Border.Bottom.Style = borderStyle;

			// Apply data formatting to entire data range at once
			if (lastRow > 1)
			{
				var dataRange = worksheet.Cells[2, 1, lastRow, colCount];
				dataRange.Style.Border.Top.Style = borderStyle;
				dataRange.Style.Border.Left.Style = borderStyle;
				dataRange.Style.Border.Right.Style = borderStyle;
				dataRange.Style.Border.Bottom.Style = borderStyle;
			}

			// Auto-fit columns using sample data for performance
			if (_formatSettings.AutoFitColumns)
			{
				int sampleEndRow = Math.Min(lastRow, _formatSettings.AutoFitSampleRows);
				worksheet.Cells[1, 1, sampleEndRow, colCount].AutoFitColumns();
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
