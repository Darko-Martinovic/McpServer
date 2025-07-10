@echo off
echo === Supermarket MCP Server - Build and Publish ===
echo.

echo Stopping any running processes...
taskkill /f /im "McpServer.exe" 2>nul
timeout /t 2 /nobreak > nul

echo Building project...
dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Publishing project...
dotnet publish --configuration Release --output "publish" --no-build
if %ERRORLEVEL% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo === Build and Publish Complete! ===
echo Published to: %~dp0publish\
echo.
echo Claude Desktop will use the published executable.
echo You can now restart Claude Desktop.
echo.
pause
