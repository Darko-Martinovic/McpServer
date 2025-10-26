@echo off
REM =============================================================================
REM Simple Azure Search Index Updater
REM =============================================================================
REM This batch file runs the server briefly to update the Azure Search index
REM =============================================================================

echo ================================================================
echo Azure Search Index Updater
echo ================================================================
echo.

echo Building project...
dotnet build --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)
echo Build successful!
echo.

echo Starting server to update Azure Search index...
echo The server will index all MCP tools including GetPluData
echo Press Ctrl+C when you see "Successfully indexed MCP tools"
echo.

dotnet run --no-launch-profile -- --webapi

echo.
echo ================================================================
echo Script completed
echo ================================================================
pause
