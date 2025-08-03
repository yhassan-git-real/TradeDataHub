# TradeDataHub.02

A WPF application for exporting trade data to Excel format. This application connects to a SQL Server database, executes stored procedures based on user-defined filters, and generates formatted Excel reports.

## Features

- **Data Filtering**: Filter by HS codes, ports, products, exporters, foreign countries, foreign names, and IEC codes
- **Date Range Selection**: Specify from/to months for data extraction
- **Report Types**: Support for both Import and Export data types
- **Excel Generation**: Automated Excel file creation with formatting and templates
- **Batch Processing**: Process multiple filter combinations in a single operation

## Prerequisites

- .NET 8.0 or later
- SQL Server access to the `Raw_Process` database
- Excel template file (`EXDPORT_Tamplate_JNPT.xlsx`)

## Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "AppSettings": {
    "Database": {
      "ConnectionString": "Server=MATRIX;Database=Raw_Process;User Id=module;Password=tcs@2015;",
      "StoredProcedureName": "ExportData_New1"
    },
    "Files": {
      "TemplatePath": "Templates/EXDPORT_Tamplate_JNPT.xlsx",
      "OutputDirectory": "EXCEL_Exports"
    },
    "Logging": {
      "LogPath": "Logs/TradeDataHub.log"
    }
  }
}
```

## Project Structure

```
TradeDataHub.02/
├── DataExporter/
│   ├── App.xaml              # Application definition
│   ├── App.xaml.cs           # Application startup logic
│   ├── AppSettings.cs        # Configuration classes
│   ├── appsettings.json      # Application configuration
│   ├── DataAccess.cs         # Database access layer
│   ├── ExcelService.cs       # Excel generation service
│   ├── MainWindow.xaml       # Main UI definition
│   ├── MainWindow.xaml.cs    # Main UI logic
│   ├── Templates/            # Excel template files
│   ├── EXCEL_Exports/        # Generated Excel files
│   └── Logs/                 # Application logs
├── .gitignore                # Git ignore rules
└── README.md                 # This file
```

## Building and Running

### Prerequisites
- Visual Studio 2022 or later
- .NET 8.0 SDK

### Build
```bash
dotnet build
```

### Run
```bash
cd DataExporter
dotnet run
```

## Usage

1. **Launch the application**
2. **Enter date range** in YYYYMM format (e.g., 202401 for January 2024)
3. **Specify filters** (comma-separated values):
   - HS Code(s)
   - Port(s)
   - Product(s)
   - Exporter(s)
   - Foreign Country(s)
   - Foreign Name(s)
   - IEC(s)
4. **Select report type**: Import or Export
5. **Click "Generate Reports"** to process and create Excel files

## Dependencies

- **Microsoft.Data.SqlClient** - SQL Server connectivity
- **ClosedXML** - Excel file generation
- **Microsoft.Extensions.Configuration** - Configuration management

## Migration Notes

This application is a modernized version of a VB6 application, converted to:
- .NET 8 WPF for the user interface
- Modern C# patterns and practices
- Improved error handling and logging
- Better configuration management

## License

[Add your license information here]