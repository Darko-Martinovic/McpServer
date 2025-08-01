# McpServer Copilot Instructions

## Architecture Overview

This is a **dual-mode** .NET 9.0 application serving supermarket inventory/sales data as either:

1. **MCP Server** (console mode): JSON-RPC protocol for AI tools like Claude Desktop
2. **Web API** (REST mode): Standard HTTP endpoints on port 9090

**Key architectural decision**: Same business logic serves both modes via shared `ISupermarketDataService` interface.

## Critical Workflow Commands

### Running the Application

```bash
# MCP mode (for Claude Desktop integration)
dotnet run --no-launch-profile -- --console

# Web API mode
dotnet run -- --webapi
# OR just: dotnet run
```

**Important**: Use `--no-launch-profile` in MCP mode to prevent stdout pollution that breaks JSON-RPC communication.

### Database Setup

```bash
# Run setup scripts in order:
# 1. Database/SetupDatabase.sql (creates tables + sample data)
# 2. Database/Phase3-PredictiveAnalytics.sql (adds analytics features)
```

## Project-Specific Patterns

### MCP Tool Implementation

MCP tools in `SupermarketMcpTools.cs` follow this pattern:

```csharp
[McpServerTool, Description("Tool description")]
public static async Task<string> ToolName(
    ISupermarketDataService dataService,
    [Description("Parameter description")] string parameter
)
{
    // Always return JSON-serialized results
    return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
}
```

### REST Endpoint Mirroring

Each MCP tool has a corresponding REST endpoint in `SupermarketController.cs` with identical business logic:

- MCP: `GetProducts()` → REST: `GET /api/supermarket/products`
- MCP: `GetLowStockProducts(threshold)` → REST: `GET /api/supermarket/products/low-stock?threshold={n}`

### Data Service Layer

All data access goes through `SupermarketDataService` implementing `ISupermarketDataService`:

- Uses parameterized SQL queries with `SqlConnection`/`SqlCommand`
- Implements request ID logging pattern: `LoggingHelper.CreateRequestId()`
- Returns empty collections on errors, never null

### Filtering Patterns

Products support multiple filtering approaches:

```csharp
// Single filters
GetProductsByCategoryAsync(string category)
GetProductsBySupplierAsync(string supplier)

// Combined filters via REST
GET /api/supermarket/inventory/detailed?category=Dairy&supplier=FreshCo
```

## Integration Points

### Claude Desktop Configuration

MCP mode requires specific stdout handling:

```csharp
// In RunMcpServerAsync()
Console.SetOut(Console.Error); // Redirects stdout to stderr
```

### Azure Search Integration

Optional indexing of MCP tools for discovery (uses dummy config to prevent initialization errors when not configured).

### Configuration Dependencies

- `appsettings.json`: SQL connection string in `ConnectionStrings:DefaultConnection`
- `.env` file: Environment variables loaded via `LoadEnvironmentVariables()`
- Hard-coded port 9090 for Web API mode to bypass configuration conflicts

## Debugging & Logging

- Serilog logging to `Logs/mcpserver.log` and console
- Debug file: `debug-tool-calls.txt` for MCP tool call tracking
- Request ID pattern throughout data layer for tracing
- Use `Console.Error.WriteLine()` for debug output in MCP mode

## Key Files Reference

- `Program.cs`: Dual-mode startup logic and service configuration
- `SupermarketMcpTools.cs`: MCP tool definitions (decorated static methods)
- `Controllers/SupermarketController.cs`: REST API endpoints
- `Services/SupermarketDataService.cs`: Data access layer with SQL queries
- `Services/Interfaces/ISupermarketDataService.cs`: Service contract
- `Database/SetupDatabase.sql`: Database schema and sample data
