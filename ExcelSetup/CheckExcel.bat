@echo off
echo ===================================================
echo  TradeDataHub Excel COM Checker
echo ===================================================
echo.
echo Checking if Excel COM is ready for TradeDataHub...
echo.

cd /d "%~dp0ExcelChecker"

if not exist "ExcelChecker.exe" (
    echo Building Excel checker...
    dotnet build --configuration Release --verbosity quiet
    if errorlevel 1 (
        echo Build failed. Make sure .NET SDK is installed.
        pause
        exit /b 1
    )
)

echo Running Excel COM check...
echo.
dotnet run --configuration Release --verbosity quiet

echo.
pause
