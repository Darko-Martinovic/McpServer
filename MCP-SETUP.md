# McpServer - Claude Desktop Integration Setup

## Overview

This is a standalone MCP (Model Context Protocol) server that provides Claude Desktop with access to supermarket inventory and sales data through SQL Server LocalDB.

## Quick Setup

### 1. Executable Location

The standalone executable is located at:

```
C:\Source\AiRun\McpServer\publish\McpServer.exe
```

**Size:** ~117MB (self-contained, no .NET runtime required)

### 2. Claude Desktop Configuration

Add this configuration to your Claude Desktop MCP settings:

**Location:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "supermarket-mcp": {
      "command": "C:\\Source\\AiRun\\McpServer\\publish\\McpServer.exe",
      "args": ["--console"],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  }
}
```

### 3. Database Requirements

- **SQL Server LocalDB** must be installed and running
- Database: `SupermarketDB` on `(localdb)\\MSSQLLocalDB`
- Sample data: 25 products with sales records

### 4. Environment Configuration

The `.env` file in the publish directory contains:

- Database connection string
- Azure Cognitive Search configuration (optional)
- Azure OpenAI configuration (optional)

## Available MCP Tools

When connected to Claude Desktop, you'll have access to these tools:

### Product Management

- `GetProducts` - Get all products in inventory
- `GetLowStockProducts` - Find products below threshold
- `GetProductsByCategory` - Filter by product category
- `GetProductsBySupplier` - Filter by supplier

### Sales Analytics

- `GetSales` - Get sales data for date range
- `GetRevenue` - Calculate total revenue
- `GetSalesByCategory` - Sales breakdown by category
- `GetDailySummary` - Daily sales summary

### Inventory Management

- `GetInventoryStatus` - Real-time inventory levels
- `GetDetailedInventory` - Detailed inventory with filters

### AI-Powered Features

- `ProcessChatMessage` - Natural language queries about inventory/sales
- `GetAvailableFunctions` - List all available AI functions

## Example Claude Queries

Once configured, you can ask Claude:

```
"What products do we have in the Dairy category?"
"Show me sales data for the last month"
"Which products are running low on stock?"
"What's our total revenue for 2025?"
"Give me a daily summary for today"
```

## File Structure

```
C:\Source\AiRun\McpServer\publish\
├── McpServer.exe          # Main executable (117MB)
├── .env                   # Environment configuration
├── appsettings.json       # Application settings
└── Logs/                  # Runtime logs
    └── mcpserver.log
```

## Troubleshooting

### Common Issues

1. **"Unexpected token 'W' Warning" in Claude Desktop**

   - **Fixed!** Updated executable properly redirects all output to stderr
   - Ensure you're using the latest built executable from `publish/`

2. **"Database connection failed"**

   - Ensure SQL Server LocalDB is running
   - Verify database exists: `(localdb)\\MSSQLLocalDB\\SupermarketDB`

3. **"MCP Server not responding"**

   - Check logs in `publish/Logs/mcpserver.log`
   - Ensure Claude Desktop config path is correct
   - Test with `test-mcp-protocol.bat` to verify JSON-RPC communication

4. **"Environment file not found"**
   - Warning now goes to stderr (won't break MCP protocol)
   - Verify `.env` exists in same directory as `McpServer.exe`
   - Check file permissions

### Log Files

- **Application logs:** `publish/Logs/mcpserver.log`
- **Debug output:** Sent to stderr (visible in Claude Desktop debug mode)

## Development vs Production

- **Development:** Run with `dotnet run -- --console` from source
- **Production:** Use `McpServer.exe --console` standalone executable
- **Web API mode:** Available via `--webapi` argument (port 5000)

## Security Notes

- Database uses Windows Integrated Security
- No network ports exposed in console mode
- Local-only communication via stdio (JSON-RPC)
- Environment variables for sensitive credentials
