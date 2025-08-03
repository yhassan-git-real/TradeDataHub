@echo off
echo Excel COM Verification Tool
echo ===========================
echo.

cd /d "%~dp0ExcelChecker"
dotnet run
echo.
pause
