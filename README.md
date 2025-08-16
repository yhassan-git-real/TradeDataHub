# TradeDataHub v2.0

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)](LICENSE)

A comprehensive desktop application for importing and exporting trade data with advanced filtering capabilities, real-time monitoring, and Excel file generation.

## 🚀 Overview

TradeDataHub is a professional WPF desktop application built on .NET 8 that provides a complete solution for processing trade data. The application connects to SQL Server databases to extract, filter, and export trade information into professionally formatted Excel files with real-time progress monitoring and comprehensive logging.

## ✨ Key Features

### 📊 Data Processing
- **Import Operations**: Process import trade data with configurable filtering
- **Export Operations**: Process export trade data with advanced parameter options
- **Real-time Monitoring**: Live progress tracking with cancellation support
- **Large Dataset Handling**: Memory-efficient streaming for processing large datasets

### 📈 Excel Generation
- **Professional Formatting**: Customizable fonts, colors, and styling
- **Auto-fit Columns**: Automatic column width adjustment for readability
- **Date Formatting**: Specialized formatting for date columns
- **Row Limit Management**: Automatic handling of Excel's 1,048,575 row limit

### 🎛️ Advanced Filtering
- **Date Range**: From Month to To Month filtering
- **HS Code**: Harmonized System code filtering
- **Product**: Product name filtering
- **IEC**: Import Export Code filtering
- **Trader**: Exporter/Importer filtering
- **Geography**: Country and port filtering
- **Custom Parameters**: Flexible name-based filtering

### 🔧 Configuration & Customization
- **Database Views**: Multiple configurable database views
- **Stored Procedures**: Selectable stored procedures for different operations
- **Output Directories**: Configurable file output locations
- **Excel Formatting**: Customizable styling and formatting options

### 📝 Monitoring & Logging
- **Comprehensive Logging**: Detailed operation logs with timestamps
- **Performance Metrics**: Timing and performance data
- **Error Handling**: Robust error reporting and recovery
- **Audit Trail**: Complete audit trail for all operations

## 🛠️ System Requirements

### Prerequisites
- **Operating System**: Windows 10/11 (64-bit)
- **Framework**: .NET 8.0 Runtime
- **Database**: SQL Server (Local or Remote)
- **Memory**: 4GB RAM minimum (8GB recommended for large datasets)
- **Storage**: 2GB free disk space for application and generated files

### Dependencies
- Microsoft.Data.SqlClient 5.2.0
- EPPlus 6.x (Excel generation)
- Microsoft.Extensions.Configuration 8.0.0

## 📦 Installation

1. **Download** the latest release from the releases section
2. **Extract** the application files to your preferred directory
3. **Configure** database connection in `Config/database.appsettings.json`
4. **Run** `TradeDataHub.exe` to start the application

### Configuration Setup

#### Database Configuration
Edit `Config/database.appsettings.json`:
```json
{
  "DatabaseConfig": {
    "ConnectionString": "Server=YOUR_SERVER;Database=YOUR_DATABASE;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true;Encrypt=false;Integrated Security=false;MultipleActiveResultSets=true;",
    "LogDirectory": "C:\\Path\\To\\Your\\Logs"
  }
}
```

#### Export Configuration
Edit `Config/export.appsettings.json` to configure:
- Output directory for export files
- Default stored procedures and views
- Custom database objects

#### Import Configuration
Edit `Config/import.appsettings.json` to configure:
- Output directory for import files
- Default stored procedures and views
- Custom database objects

## 🚀 Quick Start Guide

### Import Process
1. Launch the application
2. Select **'Import'** radio button
3. Choose database view (default: 'IMPDATA')
4. Select stored procedure (default: 'ImportJNPTData_New1')
5. Set date range using month pickers
6. Configure filter parameters (optional)
7. Click **'Generate Import Files'**
8. Monitor progress in real-time
9. Find generated files in `IMPORT_Excel` folder

### Export Process
1. Launch the application
2. Select **'Export'** radio button
3. Choose database view (default: 'EXPDATA')
4. Select stored procedure (default: 'ExportData_New1')
5. Set date range using month pickers
6. Configure filter parameters (optional)
7. Click **'Generate Export Files'**
8. Monitor progress in real-time
9. Find generated files in `EXPORT_Excel` folder

## 🎯 Usage Instructions

### User Interface Navigation
- **Mode Selection**: Use radio buttons to switch between Import/Export modes
- **Database Objects**: Select views and stored procedures from dropdown menus
- **Date Range**: Use custom month pickers for precise date selection
- **Parameters**: Fill in filter parameters as needed (supports comma-separated values)
- **Progress Monitoring**: Watch real-time progress in the monitoring panel

### Keyboard Shortcuts
- **F5**: Refresh interface and reload database objects
- **Ctrl+N**: New operation
- **Ctrl+O**: Open configuration
- **Ctrl+S**: Save current settings
- **F1**: Show help documentation
- **Alt+F4**: Exit application

### Parameter Formats
- **Date**: YYYYMM format (e.g., 202401 for January 2024)
- **Multiple Values**: Use comma-separated format (e.g., "CODE1,CODE2,CODE3")
- **Wildcards**: Partial matching supported in most text fields

## 🏗️ Architecture Overview

### Project Structure
```
TradeDataHub/
├── Core/                    # Core application logic
│   ├── Cancellation/       # Cancellation handling
│   ├── Controllers/         # MVC-style controllers
│   ├── DataAccess/         # Database access layer
│   ├── Database/           # Database models and settings
│   ├── Helpers/            # Utility helpers
│   ├── Logging/            # Logging infrastructure
│   ├── Models/             # Data models
│   ├── Parameters/         # Parameter handling
│   ├── Services/           # Business services
│   └── Validation/         # Input validation
├── Features/               # Feature modules
│   ├── Common/             # Shared components
│   ├── Export/             # Export functionality
│   ├── Import/             # Import functionality
│   └── Monitoring/         # Progress monitoring
├── Config/                 # Configuration files
├── Controls/               # Custom WPF controls
├── MainWindow/             # Main window components
├── Resources/              # Application resources
└── Themes/                 # UI themes and styles
```

### Design Patterns
- **MVC Pattern**: Controllers handle business logic
- **Service Layer**: Separated business services
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Service container for dependencies
- **Observer Pattern**: Real-time monitoring and progress updates

## ⚙️ Configuration Reference

### Excel Formatting (`Config/ExportExcelFormatSettings.json`)
```json
{
  "FontName": "Times New Roman",
  "FontSize": 10,
  "HeaderBackgroundColor": "#4F81BD",
  "BorderStyle": "Thin",
  "AutoFitSampleRows": 1000,
  "DateFormat": "dd-mmm-yy",
  "DateColumns": [3],
  "TextColumns": [1, 2, 4],
  "WrapText": false,
  "AutoFitColumns": true
}
```

### Database Objects
- **Views**: Configure multiple database views for different data perspectives
- **Stored Procedures**: Set up various stored procedures for different operations
- **Order By Columns**: Specify sorting columns for consistent output

## 📊 Output Files

### File Naming Convention
- **Export Files**: `Export_YYYYMM_YYYYMM_[Parameters]_[Timestamp].xlsx`
- **Import Files**: `Import_YYYYMM_YYYYMM_[Parameters]_[Timestamp].xlsx`

### Output Locations
- **Export Files**: `EXPORT_Excel/` directory
- **Import Files**: `IMPORT_Excel/` directory
- **Log Files**: `Logs/` directory

## 🔍 Troubleshooting

### Common Issues

#### Database Connection Errors
- Verify connection string in `database.appsettings.json`
- Check database server accessibility
- Confirm user permissions for database operations

#### Excel Generation Errors
- Ensure sufficient disk space
- Check write permissions to output directories
- Verify EPPlus library installation

#### Performance Issues
- Monitor memory usage for large datasets
- Consider splitting large date ranges
- Check database query performance

#### Row Limit Exceeded
- Excel has a limit of 1,048,575 rows
- Split date ranges or add more specific filters
- Use summary views for large datasets

### Log File Analysis
Check log files in the `Logs/` directory:
- `Export_Log_YYYYMMDD.txt`: Export operation logs
- `Import_Log_YYYYMMDD.txt`: Import operation logs
- `SkippedDatasets_YYYYMMDD.log`: Skipped dataset information

## 🤝 Contributing

### Development Setup
1. Clone the repository
2. Install .NET 8 SDK
3. Open solution in Visual Studio 2022
4. Configure database connection for development
5. Build and run the application

### Code Standards
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public methods
- Write unit tests for new functionality
- Follow MVVM pattern for UI components

### Submitting Changes
1. Create a feature branch
2. Make your changes
3. Test thoroughly
4. Update documentation
5. Submit a pull request

## 📄 License

This project is proprietary software. All rights reserved.

## 📞 Support

For support and assistance:
- Check the built-in help system (F1)
- Review log files for error details
- Contact the development team for technical support

## 📝 Release Notes

### Version 2.0
- Complete rewrite in .NET 8
- Enhanced user interface
- Improved performance and memory efficiency
- Real-time progress monitoring
- Comprehensive logging system
- Configurable database objects
- Professional Excel formatting

---

© 2025 Trade Data Hub. All rights reserved.
