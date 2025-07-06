@echo off
cd /d "D:\DotNetOpenAI\McpServer\publish"
echo Starting MCP Server with enhanced logging...
echo Working directory: %CD%
echo.
McpServer.exe
pause
