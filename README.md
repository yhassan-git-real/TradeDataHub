# TradeDataHub

A high-performance WPF application for generating trade data Excel reports from SQL Server databases. The application provides an intuitive interface for filtering trade data and creating formatted Excel exports with comprehensive logging and validation.

## 🚀 Performance Enhancement

TradeDataHub now supports **Excel COM Interop** for dramatically improved export performance:

- **Traditional Method**: 45-60 minutes for 1M rows
- **COM Interop Method**: 3-5 minutes for 1M rows
- **Performance Improvement**: **10-15x faster** for large datasets

### Quick Setup for New Machines
1. Navigate to `ExcelSetup` folder
2. Run `SetupExcelCOM.bat` as Administrator
3. Verify with `VerifyExcelCOM.ps1`

See [Excel Setup Guide](ExcelSetup/README.md) for detailed instructions.

## Application Overview

TradeDataHub is designed for trade data analysis professionals who need to extract specific trade information from databases and generate professional Excel reports. The application supports complex filtering criteria and handles large datasets efficiently.

## Key Features

### Data Processing
- **Multi-Parameter Filtering**: Filter by HS codes, ports, products, exporters, foreign countries, names, and IEC codes
- **Date Range Processing**: Extract data for specific month ranges (YYYYMM format)
- **Batch Processing**: Process multiple filter combinations in a single operation
- **Data Validation**: Built-in validation for date formats, parameter ranges, and data integrity

### Excel Generation
- **Dynamic Excel Creation**: Generates Excel files without template dependencies
- **Professional Formatting**: Automatic formatting with fonts, borders, and column sizing
- **Large Dataset Handling**: Optimized for datasets up to Excel's 1M+ row limit
- **Custom File Naming**: Intelligent file naming based on filter criteria

### User Experience
- **Real-time Progress**: Live status updates during processing
- **Comprehensive Logging**: Detailed logs for troubleshooting and audit trails
- **Error Handling**: Graceful error handling with user-friendly messages
- **Performance Monitoring**: Built-in timing and performance metrics

## Project Structure

```
TradeDataHub.02/
├── TradeDataHub/                    # Main application
│   ├── Core/                        # Core functionality
│   │   ├── Helpers/                 # Utility classes
│   │   │   ├── ParameterHelper.cs   # Centralized parameter management
│   │   │   └── FileNameHelper.cs    # File naming utilities
│   │   ├── Logging/                 # Logging infrastructure
│   │   │   └── LoggingHelper.cs     # Comprehensive logging system
│   │   └── DataAccess.cs            # Database connectivity
│   ├── Features/                    # Feature modules
│   │   └── Export/                  # Export functionality
│   │       └── ExcelService.cs      # Excel generation service
│   ├── Config/                      # Configuration
│   │   └── excelFormatting.json     # Excel formatting settings
│   ├── MainWindow.xaml              # Main user interface
│   ├── MainWindow.xaml.cs           # UI logic
│   ├── App.xaml                     # Application definition
│   ├── AppSettings.cs               # Configuration classes
│   ├── appsettings.json             # Application settings
│   ├── EXCEL_Exports/               # Generated Excel files
│   └── Logs/                        # Application logs
├── Database/                        # Database objects
│   ├── StoredProcedures/            # SQL stored procedures
│   └── Views/                       # SQL views
└── README.md                        # This documentation
```

## Configuration

### Database Configuration
Configure your SQL Server connection in `appsettings.json`:

```json
{
  "AppSettings": {
    "Database": {
      "ConnectionString": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;",
      "StoredProcedureName": "ExportData_New1",
      "ViewName": "EXPDATA",
      "OrderByColumn": "sb_DATE"
    },
    "Files": {
      "OutputDirectory": "EXCEL_Exports"
    }
  }
}
```

### Excel Formatting
Customize Excel output formatting in `Config/excelFormatting.json`:

```json
{
  "FontName": "Calibri",
  "FontSize": 11,
  "HeaderBackgroundColor": "#4472C4",
  "DateFormat": "dd/mm/yyyy",
  "AutoFitColumns": true,
  "WrapText": false,
  "DateColumns": [1, 2],
  "TextColumns": [3, 4]
}
```

## Workflow

### 1. Application Startup
- Loads configuration from `appsettings.json`
- Initializes logging system
- Connects to database
- Prepares user interface

### 2. Data Processing
- User enters filter criteria and date range
- Application validates all parameters
- Executes stored procedure with filters
- Retrieves and validates data

### 3. Excel Generation
- Creates new Excel workbook
- Applies dynamic headers from database
- Formats data according to configuration
- Saves file with intelligent naming

### 4. Completion
- Displays processing summary
- Logs detailed results
- Shows success/error status

## Usage Guide

### Basic Operation
1. **Launch TradeDataHub**
2. **Enter Date Range**: From Month and To Month in YYYYMM format (e.g., 202501)
3. **Apply Filters** (optional, comma-separated):
   - HS Codes: Product classification codes
   - Ports: Port codes or names
   - Products: Product descriptions
   - Exporters: Company names
   - Foreign Countries: Destination countries
   - Foreign Names: Foreign company names
   - IEC Codes: Import/Export codes
4. **Click "Generate Reports"**
5. **Monitor Progress**: Watch real-time status updates
6. **Review Results**: Check completion summary and generated files

### Filter Examples
- Single value: `45`
- Multiple values: `45,46,47`
- Leave blank for all data: (empty field)

### File Output
Generated files are saved to `EXCEL_Exports/` with names like:
- `45_JAN25EXP.xlsx` (single HS code)
- `45-46-47_JAN25EXP.xlsx` (multiple values)
- `ALL_JAN25EXP.xlsx` (no filters)

## Prerequisites

### System Requirements
- Windows 10 or later
- .NET 8.0 Runtime
- SQL Server access
- 4GB RAM minimum (8GB recommended for large datasets)

### Development Requirements
- Visual Studio 2022 or later
- .NET 8.0 SDK
- SQL Server access for testing

## Building and Running

### Quick Start
```bash
# Clone the repository
git clone [repository-url]
cd TradeDataHub.02

# Build the application
dotnet build

# Run the application
cd TradeDataHub
dotnet run
```

### Development Build
```bash
# Restore packages
dotnet restore

# Build in debug mode
dotnet build --configuration Debug

# Run with development settings
dotnet run --environment Development
```

## Dependencies

### Core Dependencies
- **Microsoft.Data.SqlClient**: SQL Server connectivity
- **ClosedXML**: Excel file generation and formatting
- **Microsoft.Extensions.Configuration**: Configuration management

### UI Dependencies
- **WPF (.NET 8)**: User interface framework
- **System.Threading.Tasks**: Async operations

## Troubleshooting

### Common Issues

#### Database Connection Problems
**Symptom**: Connection failed errors
**Solution**:
1. Verify connection string in `appsettings.json`
2. Ensure SQL Server is accessible
3. Check user permissions
4. Test connection using SQL Server Management Studio

#### Excel Generation Errors
**Symptom**: Excel files not created
**Solution**:
1. Check `EXCEL_Exports` folder permissions
2. Ensure sufficient disk space
3. Verify Excel formatting configuration
4. Check logs for detailed error messages

#### Large Dataset Issues
**Symptom**: Application hangs or crashes with large data
**Solution**:
1. Apply more specific filters to reduce dataset size
2. Process smaller date ranges
3. Increase system memory if possible
4. Check Excel row limit warnings in logs

#### Parameter Validation Errors
**Symptom**: "Invalid Parameters" messages
**Solution**:
1. Verify date format (YYYYMM, e.g., 202501)
2. Ensure from month ≤ to month
3. Check filter value formats
4. Review parameter requirements in logs

### Log Files
Application logs are stored in `Logs/ExportLog_YYYYMMDD.txt` and contain:
- Process start/completion times
- Parameter details
- Database execution information
- Error messages and stack traces
- Performance metrics

### Performance Tips
- Use specific filters to reduce dataset size
- Process smaller date ranges for large datasets
- Monitor memory usage during large operations
- Close other applications when processing very large datasets

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review log files for detailed error information
3. Verify configuration settings
4. Test with smaller datasets to isolate issues