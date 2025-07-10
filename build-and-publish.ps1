# Supermarket MCP Server - Build and Publish Script
# PowerShell version with better error handling

Write-Host "=== Supermarket MCP Server - Build and Publish ===" -ForegroundColor Green
Write-Host ""

# Function to stop processes gracefully
function Stop-McpProcesses {
    Write-Host "Checking for running MCP server processes..." -ForegroundColor Yellow
    
    $processes = Get-Process -Name "McpServer" -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "Found $($processes.Count) running MCP server process(es). Stopping..." -ForegroundColor Yellow
        $processes | Stop-Process -Force
        Start-Sleep -Seconds 2
        Write-Host "Processes stopped." -ForegroundColor Green
    } else {
        Write-Host "No running MCP server processes found." -ForegroundColor Green
    }
}

# Function to check if Claude Desktop is running and warn user
function Check-ClaudeDesktop {
    $claudeProcess = Get-Process -Name "Claude" -ErrorAction SilentlyContinue
    if ($claudeProcess) {
        Write-Host "WARNING: Claude Desktop is currently running!" -ForegroundColor Red
        Write-Host "For best results, please close Claude Desktop before building." -ForegroundColor Yellow
        Write-Host "Press any key to continue anyway, or Ctrl+C to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }
}

try {
    # Check for Claude Desktop
    Check-ClaudeDesktop
    
    # Stop any running MCP processes
    Stop-McpProcesses
    
    # Build the project
    Write-Host "Building project..." -ForegroundColor Cyan
    $buildResult = dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "Build successful!" -ForegroundColor Green
    
    # Publish the project
    Write-Host "Publishing project..." -ForegroundColor Cyan
    $publishResult = dotnet publish --configuration Release --output "publish" --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    Write-Host "Publish successful!" -ForegroundColor Green
    
    # Success message
    Write-Host ""
    Write-Host "=== Build and Publish Complete! ===" -ForegroundColor Green
    Write-Host "Published to: $(Get-Location)\publish\" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Restart Claude Desktop if it was running" -ForegroundColor White
    Write-Host "2. Test your MCP tools with Claude" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host "Build or publish failed. Please check the output above." -ForegroundColor Red
    exit 1
}

# Optional: Test the published executable
$testChoice = Read-Host "Would you like to test the published executable? (y/N)"
if ($testChoice -eq 'y' -or $testChoice -eq 'Y') {
    Write-Host "Testing published executable..." -ForegroundColor Cyan
    Start-Process -FilePath ".\publish\McpServer.exe" -Wait -NoNewWindow
}

Write-Host "Script completed successfully!" -ForegroundColor Green
