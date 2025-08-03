# TradeDataHub Excel COM Setup

Simple setup for Excel COM Interop to enable high-performance exports.

## Files

- **SetupExcelCOM.ps1** - Main setup script (run as Administrator)
- **VerifyExcelCOM.ps1** - Verification script to test functionality

## Quick Start

**Option 1: Easy Setup (Recommended)**
1. Double-click `Setup.bat` 
2. Click "Yes" when prompted for Administrator privileges
3. Run `VerifyExcelCOM.ps1` to test

**Option 2: Manual Setup**
1. Right-click PowerShell → "Run as Administrator"
2. Navigate to ExcelSetup folder
3. Run: `.\SetupExcelCOM.ps1`
4. Run: `.\VerifyExcelCOM.ps1` to test

## What This Enables

- Excel COM Interop for Office 365/Excel installations
- CopyFromRecordset functionality for bulk data operations
- 10-15x performance improvement for large Excel exports

## Requirements

- Excel/Office 365 installed
- Administrator privileges for registry modifications
- Windows 10+ or Windows Server 2016+

---

*For detailed deployment instructions, see DEPLOYMENT.md in the root folder.*
