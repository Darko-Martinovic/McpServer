@echo off
echo Starting McpServer in Console Mode for Claude Desktop...
echo.
echo Executable: %~dp0publish\McpServer.exe
echo Working Directory: %~dp0publish
echo.

cd /d "%~dp0publish"

if not exist "McpServer.exe" (
    echo ERROR: McpServer.exe not found in publish directory
    echo Please run build-and-publish.bat first
    pause
    exit /b 1
)

if not exist ".env" (
    echo WARNING: .env file not found
    echo Copying from parent directory...
    copy "..\\.env" ".env" >nul 2>&1
)

echo Starting MCP Server...
echo Press Ctrl+C to stop
echo.

McpServer.exe --console