# =============================================================================
# Azure Search Index Update Script
# =============================================================================
# This script runs the McpServer briefly to trigger Azure Search indexing,
# then stops it automatically.
#
# Usage: .\update-azure-search.ps1
# =============================================================================

Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host "Azure Search Index Update Utility" -ForegroundColor Cyan
Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host ""

# Load environment variables from .env file
Write-Host "Loading environment variables from .env file..." -ForegroundColor Yellow

if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match '^([^#=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, "Process")
            Write-Host "  ✓ Loaded: $key" -ForegroundColor Green
        }
    }
    Write-Host ""
} else {
    Write-Host "  ✗ .env file not found!" -ForegroundColor Red
    Write-Host ""
    exit 1
}

# Verify Azure Search configuration
$azureEndpoint = [Environment]::GetEnvironmentVariable("COGNITIVESEARCH_ENDPOINT")
$azureApiKey = [Environment]::GetEnvironmentVariable("COGNITIVESEARCH_APIKEY")

if ([string]::IsNullOrWhiteSpace($azureEndpoint) -or [string]::IsNullOrWhiteSpace($azureApiKey)) {
    Write-Host "✗ Azure Search not configured!" -ForegroundColor Red
    Write-Host "  Please set COGNITIVESEARCH_ENDPOINT and COGNITIVESEARCH_APIKEY in .env file" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "Azure Search Configuration:" -ForegroundColor Cyan
Write-Host "  Endpoint: $azureEndpoint" -ForegroundColor White
Write-Host "  API Key: $($azureApiKey.Substring(0, 10))..." -ForegroundColor White
Write-Host ""

# Build the project
Write-Host "Building McpServer..." -ForegroundColor Yellow
$buildOutput = dotnet build --verbosity quiet 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed!" -ForegroundColor Red
    Write-Host $buildOutput
    exit 1
}
Write-Host "  ✓ Build successful" -ForegroundColor Green
Write-Host ""

# Run the server to trigger indexing
Write-Host "Starting server to trigger Azure Search indexing..." -ForegroundColor Yellow
Write-Host "(Server will start, index tools, and be ready for manual stop with Ctrl+C)" -ForegroundColor Gray
Write-Host ""

# Start the server process
$serverProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --no-launch-profile -- --webapi" `
    -NoNewWindow `
    -PassThru `
    -RedirectStandardOutput "azure-search-indexing.log" `
    -RedirectStandardError "azure-search-indexing-error.log"

Write-Host "Server PID: $($serverProcess.Id)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Waiting for server to start and index tools (30 seconds)..." -ForegroundColor Yellow

# Wait for indexing to complete (give it time to start and index)
Start-Sleep -Seconds 30

# Check the log for indexing results
if (Test-Path "azure-search-indexing.log") {
    $logContent = Get-Content "azure-search-indexing.log" -Raw
    
    if ($logContent -match "Successfully indexed MCP tools") {
        Write-Host ""
        Write-Host "==============================================================================" -ForegroundColor Green
        Write-Host "✓ Azure Search Indexing Completed Successfully!" -ForegroundColor Green
        Write-Host "==============================================================================" -ForegroundColor Green
        
        # Extract tool count if available
        if ($logContent -match "Found (\d+) MCP tools to index") {
            Write-Host "  Tools Indexed: $($matches[1])" -ForegroundColor White
        }
        if ($logContent -match "Successfully indexed (\d+) MCP tools") {
            Write-Host "  Total Tools: $($matches[1])" -ForegroundColor White
        }
    } elseif ($logContent -match "Failed to index MCP tools") {
        Write-Host ""
        Write-Host "✗ Azure Search indexing failed!" -ForegroundColor Red
        
        if (Test-Path "azure-search-indexing-error.log") {
            $errorContent = Get-Content "azure-search-indexing-error.log" -Raw
            Write-Host "Error details:" -ForegroundColor Yellow
            Write-Host $errorContent -ForegroundColor Red
        }
    } else {
        Write-Host "⚠ Could not determine indexing status from logs" -ForegroundColor Yellow
        Write-Host "Check azure-search-indexing.log for details" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Server is still running. Options:" -ForegroundColor Cyan
Write-Host "  1. Press Ctrl+C to stop the server" -ForegroundColor White
Write-Host "  2. Test endpoints at http://localhost:5000" -ForegroundColor White
Write-Host "  3. View logs: azure-search-indexing.log" -ForegroundColor White
Write-Host ""
Write-Host "To verify GetPluData is indexed:" -ForegroundColor Yellow
Write-Host "  curl http://localhost:5000/api/gkapi/plu-data" -ForegroundColor Gray
Write-Host ""

# Keep script running so user can test
Write-Host "Press any key to stop the server and exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Stop the server
Write-Host ""
Write-Host "Stopping server..." -ForegroundColor Yellow
Stop-Process -Id $serverProcess.Id -Force
Write-Host "✓ Server stopped" -ForegroundColor Green
Write-Host ""
Write-Host "Logs saved to:" -ForegroundColor Cyan
Write-Host "  - azure-search-indexing.log" -ForegroundColor White
Write-Host "  - azure-search-indexing-error.log" -ForegroundColor White
Write-Host ""
