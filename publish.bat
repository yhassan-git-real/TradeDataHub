@echo off
echo TradeDataHub Publishing Script
echo ==============================

echo.
echo Cleaning project...
dotnet clean

echo.
echo Building in Release mode...
dotnet build --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo Build failed! Exiting...
    pause
    exit /b 1
)

echo.
echo Publishing self-contained application...
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output "D:\TradeDataHub_app\TradeDataHub"

if %ERRORLEVEL% NEQ 0 (
    echo Publish failed! Exiting...
    pause
    exit /b 1
)

echo.
echo Publishing single-file executable...
cd TradeDataHub
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output "D:\TradeDataHub_app\TradeDataHub_SingleFile" -p:PublishSingleFile=true

if %ERRORLEVEL% NEQ 0 (
    echo Single-file publish failed! Exiting...
    pause
    exit /b 1
)

echo.
echo ==============================
echo Publishing completed successfully!
echo.
echo Output locations:
echo - Regular: D:\TradeDataHub_app\TradeDataHub\
echo - Single-file: D:\TradeDataHub_app\TradeDataHub_SingleFile\
echo.
echo Ready-to-use executables created!
pause
