@echo off
echo Testing MCP Server JSON-RPC Communication...
echo.

cd /d "%~dp0publish"

if not exist "McpServer.exe" (
    echo ERROR: McpServer.exe not found
    exit /b 1
)

echo Sending initialize request...
echo {"jsonrpc": "2.0", "method": "initialize", "params": {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "test", "version": "1.0.0"}}, "id": 1} | McpServer.exe --console

echo.
echo Test complete. Check above for JSON response (should not see any warnings in the JSON output)
pause