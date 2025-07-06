# Test MCP Server Script
Set-Location "D:\DotNetOpenAI\McpServer\publish"
Write-Host "Testing MCP Server from: $(Get-Location)" -ForegroundColor Green
Write-Host "Starting MCP Server..." -ForegroundColor Yellow

# Start the server and capture any stderr output
$process = Start-Process -FilePath ".\McpServer.exe" -PassThru -NoNewWindow -RedirectStandardError "error.log" -RedirectStandardOutput "output.log"

Start-Sleep 2

if ($process.HasExited) {
    Write-Host "Server failed to start!" -ForegroundColor Red
    if (Test-Path "error.log") {
        Write-Host "Error output:" -ForegroundColor Red
        Get-Content "error.log"
    }
} else {
    Write-Host "Server started successfully with PID: $($process.Id)" -ForegroundColor Green
    Write-Host "Check the log files for startup messages" -ForegroundColor Yellow
    
    # Wait a moment then show logs
    Start-Sleep 1
    if (Test-Path "Logs\mcpserver$(Get-Date -Format 'yyyyMMdd').log") {
        Write-Host "`nLatest log entries:" -ForegroundColor Cyan
        Get-Content "Logs\mcpserver$(Get-Date -Format 'yyyyMMdd').log" -Tail 5
    }
}

Write-Host "`nPress any key to stop the server..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

if (!$process.HasExited) {
    $process.Kill()
    Write-Host "Server stopped." -ForegroundColor Green
}
