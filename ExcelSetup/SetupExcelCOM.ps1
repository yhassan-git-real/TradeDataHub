#Requires -RunAsAdministrator
<#
.SYNOPSIS
    TradeDataHub Excel COM Setup
    
.DESCRIPTION
    Sets up Excel COM Interop for TradeDataHub high-performance exports.
    This script enables CopyFromRecordset functionality for bulk data operations.
    
.NOTES
    Must be run as Administrator for registry modifications.
#>

Write-Host "=== TradeDataHub Excel COM Setup ===" -ForegroundColor Cyan
Write-Host "Setting up Excel COM Interop for high-performance exports..." -ForegroundColor White
Write-Host ""

# Step 1: Check Excel Installation
Write-Host "1. Checking Excel Installation..." -NoNewline
try {
    $officeRegistry = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Office\ClickToRun\Configuration" -ErrorAction SilentlyContinue
    if ($officeRegistry) {
        $version = $officeRegistry.VersionToReport
        Write-Host " [OK] Found" -ForegroundColor Green
        Write-Host "   Office Version: $version" -ForegroundColor Gray
    } else {
        Write-Host " [FAIL] Not Found" -ForegroundColor Red
        throw "Excel/Office not found. Please install Office/Excel first."
    }
} catch {
    Write-Host " [FAIL] Failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Find Excel Executable
Write-Host "2. Locating Excel Executable..." -NoNewline
$excelPaths = @(
    "C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE",
    "C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE",
    "C:\Program Files\Microsoft Office\Office16\EXCEL.EXE",
    "C:\Program Files (x86)\Microsoft Office\Office16\EXCEL.EXE"
)

$excelPath = $null
foreach ($path in $excelPaths) {
    if (Test-Path $path) {
        $excelPath = $path
        break
    }
}

if ($excelPath) {
    Write-Host " [OK] Found" -ForegroundColor Green
    Write-Host "   Path: $excelPath" -ForegroundColor Gray
} else {
    Write-Host " [FAIL] Not Found" -ForegroundColor Red
    throw "Excel executable not found in standard locations."
}

# Step 3: Create COM Registry Entries
Write-Host "3. Creating COM Registry Entries..." -NoNewline
try {
    # Create Excel.Application COM entry
    & reg add "HKEY_CLASSES_ROOT\Excel.Application" /ve /d "Microsoft Excel Application" /f *>$null
    & reg add "HKEY_CLASSES_ROOT\Excel.Application" /v "CLSID" /d "{00024500-0000-0000-C000-000000000046}" /f *>$null
    
    # Create CLSID entry
    & reg add "HKEY_CLASSES_ROOT\CLSID\{00024500-0000-0000-C000-000000000046}" /ve /d "Microsoft Excel Application" /f *>$null
    & reg add "HKEY_CLASSES_ROOT\CLSID\{00024500-0000-0000-C000-000000000046}\LocalServer32" /ve /d "`"$excelPath`" /automation" /f *>$null
    & reg add "HKEY_CLASSES_ROOT\CLSID\{00024500-0000-0000-C000-000000000046}\ProgID" /ve /d "Excel.Application" /f *>$null
    
    Write-Host " [OK] Success" -ForegroundColor Green
} catch {
    Write-Host " [FAIL] Failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Test COM Functionality
Write-Host "4. Testing COM Functionality..." -NoNewline
try {
    $excelType = [Type]::GetTypeFromProgID("Excel.Application")
    $excel = [Activator]::CreateInstance($excelType)
    $version = $excel.Version
    $excel.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
    
    Write-Host " [OK] Success" -ForegroundColor Green
    Write-Host "   Excel COM Version: $version" -ForegroundColor Gray
} catch {
    Write-Host " [FAIL] Failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "*** Setup Complete! ***" -ForegroundColor Green
Write-Host "Excel COM Interop is now ready for TradeDataHub." -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run .\CheckExcel.bat to verify functionality" -ForegroundColor Gray
Write-Host "  2. TradeDataHub can now use high-performance Excel exports" -ForegroundColor Gray
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
try {
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
} catch {
    Read-Host "Press Enter to exit"
}
