# Changelog

All notable changes to the TradeDataHub.02 project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project setup with .NET 8 WPF application
- Comprehensive .gitignore file for Visual Studio/.NET projects
- Project documentation (README.md, DEVELOPMENT.md)
- VS Code configuration files (tasks.json, launch.json)
- Configuration management with appsettings.json
- Development environment configuration
- Required directory structure (Templates, EXCEL_Exports, Logs)
- **Database view and order column configuration in appsettings.json**

### Changed
- Migrated from VB6 to C# WPF application
- Updated AppSettings.cs with required modifiers to eliminate nullable warnings
- Improved App.xaml.cs configuration loading with better error handling
- **Made database view name and order column configurable instead of hardcoded**

### Fixed
- Resolved ComboBoxItem compilation error by adding proper using statement
- Eliminated all nullable reference warnings in the build process
- **Improved database query configurability for different environments**

### Technical Details
- **Target Framework**: .NET 8.0 Windows
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQL Server with Microsoft.Data.SqlClient
- **Excel Generation**: ClosedXML library
- **Configuration**: Microsoft.Extensions.Configuration with JSON support

### Migration Notes
This version represents a complete modernization of the legacy VB6 application:
- Modern C# language features and patterns
- Async/await for non-blocking operations
- Strong typing throughout the application
- JSON-based configuration management
- Improved error handling and user feedback
- Better resource management with using statements

## [1.0.0] - TBD

### Initial Release
- Complete trade data export functionality
- Excel report generation with templates
- Multi-criteria filtering support
- Database integration with stored procedures
