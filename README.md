# Supermarket MCP Server

A modern Model Context Protocol (MCP) server for supermarket inventory and sales management with dependency injection, clean architecture, and comprehensive logging. Features real-time inventory monitoring, advanced sales analytics, and resource-like tools for AI integration.

## Features

### Core Functionality

- **Product Management**: Get all products in inventory with detailed information
- **Sales Analytics**: Retrieve sales data for specific date ranges with comprehensive reporting
- **Revenue Tracking**: Calculate total revenue for periods with detailed breakdowns
- **Inventory Monitoring**: Identify products with low stock levels and automated alerts
- **Category Analysis**: Get sales performance by product category with trend analysis

### Advanced Resource-like Tools

- **Real-time Inventory Status**: Live inventory monitoring with stock levels, reorder points, and recent sales data
- **Daily Sales Summaries**: Automated daily reporting with transaction counts, revenue, and top-performing categories
- **Detailed Inventory Reports**: Comprehensive product information for inventory management

### Technical Features

- **Comprehensive Logging**: Structured logging with Serilog to file (MCP protocol compliant)
- **MCP Protocol Compliance**: Proper JSON-RPC communication with stderr output redirection
- **Clean Architecture**: Dependency injection, service interfaces, and SOLID principles
- **Database Integration**: SQL Server connectivity with advanced queries and performance monitoring
- **Error Handling**: Robust exception handling with detailed logging and graceful degradation

## Prerequisites

- .NET 9.0 or later
- SQL Server 2014 or later
- Node.js (for MCP Inspector testing)

## Setup

1. **Database Setup**: Run the database scripts in the `Database/` folder:

   - `CreateDatabase.sql` - Creates the database and tables
   - `SampleData.sql` - Inserts sample data for testing
   - `SetupComplete.sql` - Complete setup script

2. **Update Connection String**: Edit `appsettings.json` and replace the connection string with your SQL Server details:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-server-name;Database=your-database-name;Integrated Security=true;TrustServerCertificate=true;"
     }
   }
   ```

3. **Database Schema**: The database includes the following tables with enhanced functionality:
   - `Products` (ProductId, ProductName, Category, Price, StockQuantity, Supplier, ReorderLevel, LastUpdated)
   - `Sales` (SaleId, ProductId, Quantity, UnitPrice, TotalAmount, SaleDate)

## Available Tools

### Core Supermarket Tools

### GetProducts

Retrieves all products in the supermarket inventory with complete product information.

### GetSalesData

Gets comprehensive sales data for a specific date range with product details.

- Parameters: `startDate` (YYYY-MM-DD), `endDate` (YYYY-MM-DD)

### GetTotalRevenue

Calculates total revenue for a date range with detailed analytics.

- Parameters: `startDate` (YYYY-MM-DD), `endDate` (YYYY-MM-DD)

### GetLowStockProducts

Identifies products with stock levels below a threshold for inventory management.

- Parameters: `threshold` (default: 10)

### GetSalesByCategory

Analyzes sales performance by product category with detailed metrics.

- Parameters: `startDate` (YYYY-MM-DD), `endDate` (YYYY-MM-DD)

### Resource-like Tools (Real-time Data)

### GetInventoryStatus

**Real-time inventory monitoring** with comprehensive status information including:

- Current stock levels and reorder points
- Stock status classification (Out of Stock, Low Stock, Medium Stock, In Stock)
- Recent sales data (last 7 days)
- Last updated timestamps
- Organized by category for easy management

### GetDailySummary

**Daily sales summary** with comprehensive business intelligence including:

- Total transactions and revenue for the day
- Number of unique products sold
- Total items sold count
- Average transaction value
- Top-performing category and its revenue
- Configurable date parameter (defaults to today)
- Parameters: `date` (YYYY-MM-DD, optional - defaults to today)

### GetDetailedInventory

**Detailed inventory information** providing complete product catalog with:

- Full product details and specifications
- Current inventory levels
- Pricing information
- Supplier details
- Ideal for comprehensive inventory audits and reporting
  Calculates total revenue for a date range.
- Parameters: `startDate` (YYYY-MM-DD), `endDate` (YYYY-MM-DD)

### GetLowStockProducts

Identifies products with stock levels below a threshold.

- Parameters: `threshold` (default: 10)

### GetSalesByCategory

Analyzes sales performance by product category.

- Parameters: `startDate` (YYYY-MM-DD), `endDate` (YYYY-MM-DD)

## Testing with MCP Inspector

1. Install the MCP Inspector:

   ```bash
   npm install -g @modelcontextprotocol/inspector
   ```

2. Run the MCP server with the inspector:

   ```bash
   npx @modelcontextprotocol/inspector dotnet run --project .
   ```

3. Open the web interface provided by the inspector to test the tools.

## Configuring Claude from Anthropic as MCP Client

To use this MCP server with Claude from Anthropic, you need to configure Claude's MCP settings. Here are two configuration examples:

### Development Configuration (Using dotnet run)

For development and testing, use the `dotnet run` command:

```json
{
  "mcpServers": {
    "supermarket": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/your/McpServer", "--no-build"]
    }
  }
}
```

**Note**: Replace `/path/to/your/McpServer` with the actual path to your project folder.

### Production Configuration (Using Published Application)

For production deployment, first publish the application:

```bash
dotnet publish -c Release -o publish
```

Then use the published executable in your MCP configuration:

```json
{
  "mcpServers": {
    "supermarket": {
      "command": "/path/to/your/McpServer/publish/McpServer.exe"
    }
  }
}
```

**Note**: Replace `/path/to/your/McpServer/publish/McpServer.exe` with the actual path to your published executable.

### Configuration Steps

1. **Open Claude's MCP Settings**: In Claude's interface, navigate to the MCP configuration section
2. **Add the Configuration**: Paste the appropriate JSON configuration above
3. **Update Paths**: Replace the placeholder paths with your actual project or published directory paths
4. **Save and Restart**: Save the configuration and restart Claude to load the MCP server

### Verification

After configuration, Claude should be able to access all supermarket tools:

**Core Tools:**

- GetProducts
- GetSalesData
- GetTotalRevenue
- GetLowStockProducts
- GetSalesByCategory

**Resource-like Tools (Real-time Data):**

- GetInventoryStatus
- GetDailySummary
- GetDetailedInventory

### Editing the Claude Desktop Configuration

To use the MCP server with the Anthropic Claude Desktop app, you must edit the configuration file:

- **File name:** `claude_desktop_config.json`
- **Typical location:**  
  `C:\Users\<YourUsername>\AppData\Roaming\Claude`

Open this file in a text editor and add or update the MCP server configuration as shown in the examples above.
After saving your changes, restart the Claude Desktop app for the new settings to take effect.

## Testing Scripts

The project includes testing scripts for different environments:

### PowerShell Testing

```powershell
.\test-mcp-server.ps1
```

### Batch Testing

```cmd
test-mcp-server.bat
```

These scripts help verify the MCP server is running correctly and show startup logs.

## Logging

The application uses Serilog for comprehensive logging:

- **File Logging**: Logs are written to `Logs/mcpserver.log` with daily rotation
- **Structured Logging**: JSON-formatted logs with request correlation IDs
- **Log Levels**: Configurable through `appsettings.json`
- **MCP Protocol Compliance**: All console output redirected to stderr to avoid breaking JSON-RPC communication

### Log Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/mcpserver.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

## Architecture

The application uses:

- **Clean Architecture**: Organized into Models, Services, and Configuration
- **Dependency Injection**: Configured with Microsoft.Extensions.Hosting
- **Configuration**: JSON-based configuration with appsettings.json
- **Data Access**: Microsoft.Data.SqlClient for SQL Server connectivity
- **MCP Protocol**: ModelContextProtocol for AI tool integration
- **Structured Logging**: Serilog with file and console sinks

## Project Structure

```
McpServer/
├── Models/                          # Data models
│   ├── Product.cs                   # Product entity
│   ├── SalesRecord.cs               # Sales record entity
│   ├── CategorySales.cs             # Category sales summary
│   ├── InventoryStatus.cs           # Real-time inventory status model
│   └── DailySummary.cs              # Daily sales summary model
├── Services/                        # Business logic and data access
│   ├── Interfaces/                  # Service interfaces
│   │   └── ISupermarketDataService.cs # Extended with resource methods
│   ├── SupermarketDataService.cs    # SQL Server implementation with advanced queries
│   └── LoggingHelper.cs             # Logging utilities
├── Configuration/                   # Configuration classes
│   └── ConnectionStringOptions.cs   # Database connection options
├── Database/                        # Database setup scripts
│   ├── CreateDatabase.sql           # Database creation script
│   ├── SampleData.sql               # Sample data insertion
│   └── SetupComplete.sql            # Complete setup script
├── Program.cs                       # Application entry point and DI setup
├── SupermarketMcpTools.cs           # MCP tools definitions (8 tools total)
├── appsettings.json                 # Configuration file
├── test-mcp-server.ps1              # PowerShell testing script
├── test-mcp-server.bat              # Batch testing script
└── README.md                        # Project documentation
```

### Namespace Organization

- **`McpServer.Models`**: Data entities and DTOs
- **`McpServer.Services`**: Business logic implementations
- **`McpServer.Services.Interfaces`**: Service contracts
- **`McpServer.Configuration`**: Configuration options

## Building and Running

```bash
# Build the project
dotnet build

# Run the MCP server
dotnet run

# Publish for deployment
dotnet publish -c Release -o publish

# Test with MCP Inspector
npx @modelcontextprotocol/inspector dotnet run --project .
```

## Deployment

1. **Publish the application**:

   ```bash
   dotnet publish -c Release -o publish
   ```

2. **Copy configuration**: Ensure `appsettings.json` is in the publish directory

3. **Run the executable**:
   ```bash
   cd publish
   ./McpServer.exe
   ```

## Dependencies

- **ModelContextProtocol**: 0.3.0-preview.2 - MCP server framework
- **Microsoft.Data.SqlClient**: 6.0.2 - SQL Server connectivity
- **Microsoft.Extensions.Hosting**: 9.0.6 - Dependency injection and hosting
- **Serilog.AspNetCore**: 9.0.0 - Structured logging
- **Serilog.Settings.Configuration**: 9.0.0 - Configuration-based logging setup
- **Serilog.Sinks.File**: 7.0.0 - File logging sink

## Integration with AI Applications

This MCP server can be integrated with AI applications that support the Model Context Protocol, providing them with access to supermarket data through the defined tools. All tools return JSON-formatted data for easy consumption by AI systems.

### Advanced Query Features

The server implements sophisticated SQL queries for real-time analytics:

- **Inventory Status Queries**: Complex joins with sales data to provide recent sales trends
- **Daily Summary Analytics**: Common Table Expressions (CTEs) for aggregated reporting
- **Performance Optimization**: Indexed queries with proper parameterization
- **Real-time Data**: Live database connections for up-to-date information

### MCP Resources Implementation

Originally designed to support MCP Resources, the server now implements resource-like functionality through specialized tools:

- **GetInventoryStatus**: Provides real-time inventory data equivalent to `supermarket://inventory/status` resource
- **GetDailySummary**: Offers daily reporting equivalent to `supermarket://sales/daily-summary` resource
- **GetDetailedInventory**: Delivers comprehensive inventory equivalent to `supermarket://inventory/detailed` resource

This approach ensures compatibility with current MCP library versions while providing the same functionality.

### Logging and Monitoring

- **Request Correlation IDs**: Each operation gets a unique identifier for tracing
- **Performance Metrics**: Database operation timing and result counts
- **Error Tracking**: Comprehensive exception logging with stack traces
- **MCP Protocol Compliance**: All output properly routed to avoid JSON-RPC interference

## Troubleshooting

### Common Issues

1. **Database Connection**: Ensure SQL Server is running and the connection string is correct
2. **MCP Protocol Errors**: Check that no console output is going to stdout (should all go to stderr)
3. **Logging Issues**: Verify the `Logs/` directory exists and is writable
4. **Tool Execution**: Check the log files for detailed error information
5. **Resource Tools Not Appearing**: The resource-like tools (GetInventoryStatus, GetDailySummary, GetDetailedInventory) appear as regular tools in MCP Inspector
6. **Database Schema Issues**: Ensure your Products table has ReorderLevel and LastUpdated columns for inventory status queries

### Debug Information

- Logs are stored in `Logs/mcpserver.log` with daily rotation
- Debug tool calls are logged to `debug-tool-calls.txt`
- Startup information is written to stderr for debugging
- Each database operation includes timing and result count information
- Request correlation IDs help trace operations across logs

### Performance Tips

- **Database Indexing**: Ensure proper indexes on ProductId, SaleDate, and Category columns
- **Connection Pooling**: SQL Server connection pooling is automatically handled
- **Query Optimization**: All queries use parameterized statements for security and performance
- **Logging Level**: Adjust logging level in appsettings.json for production environments

## Benefits of the Architecture

- **Separation of Concerns**: Models, services, and configuration are clearly separated
- **Maintainability**: Each class has a single responsibility
- **Testability**: Interfaces allow for easy unit testing with mocks
- **Scalability**: Easy to add new models, services, or tools
- **Clean Architecture**: Follows SOLID principles and clean architecture patterns
- **Production Ready**: Comprehensive logging and error handling
- **MCP Compliance**: Proper protocol implementation for AI tool integration
- **Real-time Analytics**: Advanced SQL queries provide live business intelligence
- **Extensible Design**: Resource-like tools demonstrate how to add sophisticated functionality
- **Database Performance**: Optimized queries with proper indexing and parameterization
- **Robust Error Handling**: Graceful degradation with detailed error logging

## Future Extensibility

The current architecture supports easy extension with additional features:

### Potential Enhancements

- **MCP Prompts**: Pre-defined prompts for common business scenarios
- **Streaming Data**: Real-time updates for inventory changes
- **Audit Logging**: Track all data modifications and access
- **Legacy System Integration**: Adapters for existing retail systems
- **Advanced Analytics**: Machine learning integration for demand forecasting
- **Multi-tenant Support**: Support for multiple store locations
- **Caching Layer**: Redis integration for improved performance
- **API Versioning**: Support for multiple API versions

### Adding New Tools

1. Define new models in the `Models/` folder
2. Add service methods to `ISupermarketDataService`
3. Implement methods in `SupermarketDataService`
4. Add MCP tool methods to `SupermarketMcpTools.cs`
5. Update documentation and tests

This modular approach ensures the system can grow with your business requirements while maintaining clean architecture principles.
