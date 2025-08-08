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

namespace TradeDataHub.Features.Export;

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

// Renamed from ExcelService to ExportExcelService for clarity
public class ExportExcelService
{
	private readonly ExcelFormatSettings _formatSettings;
	private readonly ModuleLogger _logger;
	private readonly ExportSettings _exportSettings;
	private readonly SharedDatabaseSettings _dbSettings;

	public ExportExcelService()
	{
		_formatSettings = LoadExcelFormatSettings();
		_logger = ModuleLoggerFactory.GetExportLogger();
		_exportSettings = LoadExportSettings();
		_dbSettings = LoadSharedDatabaseSettings();
	}

	private ExcelFormatSettings LoadExcelFormatSettings()
	{
		const string jsonFileName = "Config/excelFormatting.json";
		if (!File.Exists(jsonFileName))
			throw new FileNotFoundException($"Excel formatting file '{jsonFileName}' not found.");
		var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(jsonFileName, false);
		var config = builder.Build();
		return config.Get<ExcelFormatSettings>()!;
	}

	private ExportSettings LoadExportSettings()
	{
		const string json = "Config/export.appsettings.json";
		var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json, false);
		var cfg = builder.Build();
		var root = cfg.Get<ExportSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind ExportSettingsRoot");
		return root.ExportSettings;
	}

	private SharedDatabaseSettings LoadSharedDatabaseSettings()
	{
		const string json = "Config/database.appsettings.json";
		var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(json, false);
		var cfg = builder.Build();
		var root = cfg.Get<SharedDatabaseSettingsRoot>() ?? throw new InvalidOperationException("Failed to bind SharedDatabaseSettingsRoot");
		return root.DatabaseConfig;
	}

	public ExcelResult CreateReport(int combinationNumber, string fromMonth, string toMonth, string hsCode, string product, string iec, string exporter, string country, string name, string port)
	{
		var processId = _logger.GenerateProcessId();

		var parameterSet = ParameterHelper.CreateExportParameterSet(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
		var parameterDisplay = ParameterHelper.FormatParametersForDisplay(parameterSet);
		_logger.LogProcessStart("Excel Export Generation", parameterDisplay, processId);

		using var reportTimer = _logger.StartTimer("Total Process", processId);
		var dataAccess = new ExportDataAccess();
		try
		{
			_logger.LogStep("Database", "Executing stored procedure", processId);
			using var spTimer = _logger.StartTimer("Stored Procedure", processId);
			var (connection, reader, recordCount) = dataAccess.GetDataReader(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
			spTimer.Dispose();
			try
			{
				_logger.LogStep("Validation", $"Row count: {recordCount:N0}", processId);
				if (recordCount == 0)
				{
					ModuleSkippedDatasetLogger.LogExportSkippedDataset(combinationNumber, 0, fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, "NoData");
					_logger.LogProcessComplete("Excel Export Generation", reportTimer.Elapsed, "No data - skipped", processId);
					return new ExcelResult { Success = false, SkipReason = SkipReason.NoData, RowCount = 0 };
				}
				if (recordCount > 1_048_575)
				{
					string skippedFileName = FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
					ModuleSkippedDatasetLogger.LogExportSkippedDataset(combinationNumber, recordCount, fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port, "RowLimit");
					_logger.LogSkipped(skippedFileName, recordCount, "Excel row limit exceeded", processId);
					_logger.LogProcessComplete("Excel Export Generation", reportTimer.Elapsed, "Skipped - too many rows", processId);
					return new ExcelResult { Success = false, SkipReason = SkipReason.ExcelRowLimit, FileName = skippedFileName, RowCount = (int)recordCount };
				}
				string fileName = FileNameHelper.GenerateExportFileName(fromMonth, toMonth, hsCode, product, iec, exporter, country, name, port);
				_logger.LogExcelFileCreationStart(fileName, processId);
				using var excelTimer = _logger.StartTimer("Excel Creation", processId);
				using var package = new ExcelPackage();
				var worksheetName = _exportSettings.Operation.WorksheetName;
				var worksheet = package.Workbook.Worksheets.Add(string.IsNullOrWhiteSpace(worksheetName) ? "Export Data" : worksheetName);
				int fieldCount = reader.FieldCount;
				worksheet.Cells.Style.Font.Name = _formatSettings.FontName;
				worksheet.Cells.Style.Font.Size = _formatSettings.FontSize;
				worksheet.Cells.Style.WrapText = _formatSettings.WrapText;
				foreach (int dateCol in _formatSettings.DateColumns)
					if (dateCol > 0 && dateCol <= fieldCount)
						worksheet.Column(dateCol).Style.Numberformat.Format = _formatSettings.DateFormat;
				foreach (int textCol in _formatSettings.TextColumns)
					if (textCol > 0 && textCol <= fieldCount)
						worksheet.Column(textCol).Style.Numberformat.Format = "@";
				for (int col = 0; col < fieldCount; col++)
					worksheet.Cells[1, col + 1].Value = reader.GetName(col);
				worksheet.Cells[2, 1].LoadFromDataReader(reader, false);
				int lastRow = (int)recordCount + 1; // include header
				ApplyExcelFormatting(worksheet, lastRow, fieldCount);
				string outputDir = _exportSettings.Files.OutputDirectory;
				Directory.CreateDirectory(outputDir);
				string outputPath = Path.Combine(outputDir, fileName);
				using var saveTimer = _logger.StartTimer("File Save", processId);
				package.SaveAs(new FileInfo(outputPath));
				_logger.LogFileSave("Completed", saveTimer.Elapsed, processId);
				saveTimer.Dispose();
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
		catch (Exception ex)
		{
			_logger.LogError($"Process failed: {ex.Message}", ex, processId);
			_logger.LogProcessComplete("Excel Export Generation", reportTimer.Elapsed, "Failed with error", processId);
			throw;
		}
	}

	private void ApplyExcelFormatting(ExcelWorksheet worksheet, int lastRow, int colCount)
	{
		if (lastRow <= 1) return;
		var borderStyle = (_formatSettings.BorderStyle?.Equals("none", StringComparison.OrdinalIgnoreCase) == true)
			? ExcelBorderStyle.None : ExcelBorderStyle.Thin;
		var headerRange = worksheet.Cells[1, 1, 1, colCount];
		headerRange.Style.Font.Bold = true;
		headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
		headerRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(_formatSettings.HeaderBackgroundColor));
		headerRange.Style.Border.Top.Style = borderStyle;
		headerRange.Style.Border.Left.Style = borderStyle;
		headerRange.Style.Border.Right.Style = borderStyle;
		headerRange.Style.Border.Bottom.Style = borderStyle;
		if (lastRow > 1)
		{
			var dataRange = worksheet.Cells[2, 1, lastRow, colCount];
			dataRange.Style.Border.Top.Style = borderStyle;
			dataRange.Style.Border.Left.Style = borderStyle;
			dataRange.Style.Border.Right.Style = borderStyle;
			dataRange.Style.Border.Bottom.Style = borderStyle;
		}
		if (_formatSettings.AutoFitColumns)
		{
			int sampleLimit = _formatSettings.AutoFitSampleRows > 0 ? _formatSettings.AutoFitSampleRows : 1000;
			int sampleEndRow = Math.Min(lastRow, sampleLimit);
			worksheet.Cells[1, 1, sampleEndRow, colCount].AutoFitColumns();
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
