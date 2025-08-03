# TradeDataHub.02 Development Guide

## Project Overview

This is a WPF application that migrates functionality from a legacy VB6 system for trade data export operations.

## Original VB6 Logic Analysis

Based on the provided VB6 code (`EXP_Module.vbp`), the application performs the following operations:

### Key Functionality
1. **Database Connection**: Connects to SQL Server using SQLNCLI11 provider
2. **Filter Processing**: Splits comma-separated filter values for multiple criteria
3. **Nested Loops**: Processes all combinations of filters (ports, HS codes, products, etc.)
4. **Stored Procedure Execution**: Calls `ExportData_New1` stored procedure
5. **Excel Generation**: Creates Excel files using templates and recordset data
6. **File Naming**: Generates descriptive filenames based on filters and date ranges

### Migration Improvements

#### From VB6 to C# WPF:
- **Modern UI**: WPF instead of VB6 forms
- **Async Operations**: Non-blocking UI during data processing
- **Better Error Handling**: Try-catch blocks with user feedback
- **Configuration Management**: JSON-based configuration
- **Dependency Injection**: Modern patterns for service management
- **Type Safety**: Strong typing throughout the application

#### Performance Enhancements:
- **Async/Await**: Non-blocking database operations
- **Using Statements**: Proper resource disposal
- **Parameterized Queries**: SQL injection prevention
- **Bulk Operations**: Efficient data processing

## Architecture

### Layers:
1. **Presentation Layer**: MainWindow.xaml/cs
2. **Business Logic**: MainWindow event handlers
3. **Data Access Layer**: DataAccess.cs
4. **Service Layer**: ExcelService.cs
5. **Configuration**: AppSettings.cs

### Key Classes:

#### `DataAccess.cs`
- Database connectivity
- Stored procedure execution
- Data retrieval operations

#### `ExcelService.cs`
- Excel template loading
- Data formatting and export
- File generation and saving

#### `AppSettings.cs`
- Configuration model classes
- Database connection settings
- File path configurations

## Development Workflow

### 1. Database Setup
- Ensure SQL Server is accessible
- Verify stored procedure `ExportData_New1` exists
- Test database connection with provided credentials

### 2. Template Setup
- Place Excel template in `Templates/` directory
- Ensure template format matches expected data structure

### 3. Configuration
- Update `appsettings.json` with appropriate connection strings
- Configure file paths for templates and output directories

### 4. Testing
- Test with small data sets initially
- Verify Excel output format and data accuracy
- Test various filter combinations

## Deployment

### Requirements:
- .NET 8.0 Runtime
- SQL Server access
- Excel template files
- Appropriate file system permissions

### Deployment Steps:
1. Publish the application: `dotnet publish -c Release`
2. Copy template files to deployment directory
3. Configure production connection strings
4. Create necessary output directories
5. Set up logging directory with write permissions

## Troubleshooting

### Common Issues:
1. **Database Connection**: Verify connection string and credentials
2. **Template Not Found**: Check template file path in configuration
3. **Permission Errors**: Ensure write access to output directories
4. **Excel Generation**: Verify ClosedXML package installation

### Logging:
- Application logs are written to the configured log path
- Check logs for detailed error information
- Enable debug logging for development environments

## Future Enhancements

### Potential Improvements:
1. **Progress Indicators**: Better user feedback during processing
2. **Parallel Processing**: Multiple Excel files generation
3. **Caching**: Database result caching for repeated queries
4. **Email Integration**: Automatic report distribution
5. **Scheduling**: Automated report generation
6. **Web Interface**: Browser-based access
7. **API Endpoints**: REST API for external integration
