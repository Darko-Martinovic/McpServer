# =============================================================================
# Quick Azure Search Index Status Checker
# =============================================================================
# This script queries the Azure Search index to see what tools are indexed
# =============================================================================

param(
    [switch]$Update,
    [switch]$List
)

# Load .env file
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match '^([^#=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, "Process")
        }
    }
}

$endpoint = [Environment]::GetEnvironmentVariable("COGNITIVESEARCH_ENDPOINT")
$apiKey = [Environment]::GetEnvironmentVariable("COGNITIVESEARCH_APIKEY")
$indexName = "mcp-tools"

if (!$endpoint -or !$apiKey) {
    Write-Host "Error: Azure Search not configured in .env file" -ForegroundColor Red
    exit 1
}

Write-Host "Azure Search Index: $indexName" -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint" -ForegroundColor Gray
Write-Host ""

# Function to query the index
function Get-IndexedTools {
    $url = "$endpoint/indexes/$indexName/docs/search?api-version=2021-04-30-Preview"
    
    $body = @{
        search = "*"
        select = "id,functionName,endpoint,category,lastUpdated"
        top = 100
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri $url -Method Post -Body $body `
            -Headers @{
                "api-key" = $apiKey
                "Content-Type" = "application/json"
            }
        
        return $response.value
    } catch {
        Write-Host "Error querying index: $_" -ForegroundColor Red
        return $null
    }
}

if ($List -or !$Update) {
    Write-Host "Querying indexed tools..." -ForegroundColor Yellow
    $tools = Get-IndexedTools
    
    if ($tools) {
        Write-Host "Found $($tools.Count) tools in index:" -ForegroundColor Green
        Write-Host ""
        
        # Group by category
        $gkapiTools = $tools | Where-Object { $_.endpoint -like "*/gkapi/*" }
        $supermarketTools = $tools | Where-Object { $_.endpoint -like "*/supermarket/*" }
        
        Write-Host "GkApi Tools ($($gkapiTools.Count)):" -ForegroundColor Cyan
        $gkapiTools | Sort-Object functionName | ForEach-Object {
            $indicator = if ($_.functionName -eq "GetPluData") { " ← NEW" } else { "" }
            Write-Host "  • $($_.functionName)$indicator" -ForegroundColor $(if ($indicator) { "Green" } else { "White" })
            Write-Host "    $($_.endpoint)" -ForegroundColor Gray
        }
        
        Write-Host ""
        Write-Host "Supermarket Tools ($($supermarketTools.Count)):" -ForegroundColor Cyan
        $supermarketTools | Sort-Object functionName | ForEach-Object {
            Write-Host "  • $($_.functionName)" -ForegroundColor White
            Write-Host "    $($_.endpoint)" -ForegroundColor Gray
        }
        
        Write-Host ""
        
        # Check if GetPluData is indexed
        $pluTool = $tools | Where-Object { $_.functionName -eq "GetPluData" }
        if ($pluTool) {
            Write-Host "✓ GetPluData IS indexed!" -ForegroundColor Green
            Write-Host "  Endpoint: $($pluTool.endpoint)" -ForegroundColor White
            Write-Host "  Last Updated: $($pluTool.lastUpdated)" -ForegroundColor Gray
        } else {
            Write-Host "✗ GetPluData NOT found in index" -ForegroundColor Red
            Write-Host "  Run with -Update flag to reindex" -ForegroundColor Yellow
        }
    } else {
        Write-Host "No tools found in index (or error occurred)" -ForegroundColor Yellow
    }
}

if ($Update) {
    Write-Host ""
    Write-Host "Starting server to update index..." -ForegroundColor Yellow
    Write-Host "This will take about 30 seconds..." -ForegroundColor Gray
    Write-Host ""
    
    # Run the dedicated update script
    & "$PSScriptRoot\update-azure-search.ps1"
}

Write-Host ""
Write-Host "Usage:" -ForegroundColor Cyan
Write-Host "  .\check-azure-search.ps1           # Check current index" -ForegroundColor White
Write-Host "  .\check-azure-search.ps1 -List     # List all tools" -ForegroundColor White
Write-Host "  .\check-azure-search.ps1 -Update   # Update index" -ForegroundColor White
Write-Host ""
