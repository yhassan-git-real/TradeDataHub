@echo off
echo ===================================================
echo  TradeDataHub Excel COM Setup
echo ===================================================
echo.
echo This will setup Excel COM Interop for high-performance exports.
echo You need Administrator privileges for this setup.
echo.
pause

echo.
echo Starting setup process...
echo.

REM Request administrator privileges and wait for completion
powershell -Command "Start-Process PowerShell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0SetupExcelCOM.ps1\" -NoExit' -Verb RunAs -Wait"

echo.
echo Setup process completed.
echo.
echo You can now run: .\VerifyExcelCOM.ps1 to test functionality
echo.
pause
