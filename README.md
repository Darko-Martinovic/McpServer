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

### 1. Database Setup

The database setup has been consolidated into two main scripts:

- **`Database/SetupDatabase.sql`** - Complete database setup script that:

  - Creates the SupermarketDB database
  - Creates all tables with enhanced schema (Products, Sales)
  - Adds required columns for MCP Resource tools (ReorderLevel, LastUpdated, UnitPrice)
  - Creates performance indexes
  - Inserts realistic sample data with proper prices
  - Handles schema migrations from older versions

- **`Database/TestDatabase.sql`** - Comprehensive verification script that:
  - Tests all database structures and columns
  - Verifies all MCP tool queries execute successfully
  - Performs data quality checks
  - Shows inventory and sales summaries
  - Confirms the database is ready for AI integration

**To setup the database:**

```bash
# Run the complete setup (from the Database folder)
sqlcmd -S "your-server-name" -i "SetupDatabase.sql"

# Verify the setup
sqlcmd -S "your-server-name" -i "TestDatabase.sql"
```

### 2. Update Connection String

Edit `appsettings.json` and replace the connection string with your SQL Server details:

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

4. **Enhanced Database Schema (Phase 3)**: New schemas organize related functionality:
   - `inventory.*` - Inventory movements and reorder history
   - `analytics.*` - Demand forecasts, seasonal patterns, and stockout risks
   - `reporting.*` - Business intelligence snapshots and reporting
   - `sales.*` - Advanced sales analytics (reserved for future use)

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

### Phase 3: Predictive Analytics Tools

**Advanced business intelligence and forecasting capabilities:**

### PredictDemand

Predicts product demand for upcoming days with confidence levels and trend analysis.

- Parameters: `daysAhead` (1-30, default: 7)
- Returns: Demand forecasts with confidence levels, trend direction, and recommended stock levels
- Uses: Historical sales data, seasonal patterns, and advanced forecasting algorithms

### GetStockoutRisks

Identifies products at risk of stockout with risk levels and recommended actions.

- Parameters: `daysAhead` (1-60, default: 14)
- Returns: Risk assessments with scores, estimated stockout dates, and potential revenue impact
- Features: High/Medium/Low risk categorization with actionable recommendations

### GetSeasonalTrends

Analyzes seasonal sales trends and patterns by category with monthly forecasts.

- Parameters: `category` (optional - analyzes all categories if not specified)
- Returns: Seasonal patterns, seasonality factors, and trend classifications
- Applications: Strategic planning, inventory optimization, seasonal preparation

### GetReorderRecommendations

Generates intelligent reorder recommendations based on demand prediction and risk analysis.

- Returns: Prioritized recommendations with quantities, timing, and reasoning
- Categories: IMMEDIATE, URGENT, SCHEDULED, MONITOR
- Integrates: Demand forecasts, stockout risks, and current inventory levels

### GetCriticalAlerts

Provides high-priority alerts for items requiring immediate attention.

- Returns: Combined critical stockout risks and urgent reorder recommendations
- Features: Real-time alerting, priority scoring, and executive summary format
- Use cases: Daily operations management, emergency response, proactive intervention

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

## Asking Questions to Claude Desktop

Once the MCP server is configured with Claude Desktop, you can ask various types of questions to leverage the supermarket data. Here are examples organized from basic single-tool queries to advanced multi-tool combinations.

### 🔵 **Basic Questions (Single Tool)**

#### Product Information

- _"What products do you have in your inventory?"_
- _"Show me all products in the dairy category"_
- _"What's the price of milk products?"_

#### Sales Analysis

- _"What were the sales for last week?"_ (GetSalesData)
- _"Show me total revenue for the month of June 2025"_ (GetTotalRevenue)
- _"Which categories performed best last month?"_ (GetSalesByCategory)

#### Inventory Management

- _"What products are running low on stock?"_ (GetLowStockProducts)
- _"Show me current inventory status"_ (GetInventoryStatus)
- _"What's today's sales summary?"_ (GetDailySummary)

### 🟡 **Intermediate Questions (2-3 Tools)**

#### Stock and Sales Correlation

- _"Show me low stock products and their recent sales performance"_
  - Uses: GetLowStockProducts + GetInventoryStatus
- _"What products need restocking based on current inventory and recent sales?"_
  - Uses: GetInventoryStatus + GetSalesData

#### Performance Analysis

- _"Compare today's sales with inventory levels - what's selling well?"_
  - Uses: GetDailySummary + GetInventoryStatus
- _"Show me products with high inventory but low recent sales"_
  - Uses: GetDetailedInventory + GetSalesData

### 🔴 **Advanced Questions (Multiple Tool Combinations)**

#### Comprehensive Business Intelligence

- _"Give me a complete business overview: current inventory status, today's performance, products needing attention, and category analysis"_

  - Uses: GetInventoryStatus + GetDailySummary + GetLowStockProducts + GetSalesByCategory

- _"Analyze our business health: what's selling well, what's not moving, what needs restocking, and revenue trends"_
  - Uses: GetInventoryStatus + GetSalesData + GetTotalRevenue + GetDetailedInventory

#### Strategic Decision Making

- _"I'm meeting with suppliers tomorrow - what should I order based on current stock, recent sales trends, and revenue impact?"_

  - Uses: GetInventoryStatus + GetSalesData + GetTotalRevenue + GetSalesByCategory

- _"Create a dashboard view showing inventory alerts, sales performance, and revenue analysis for management"_
  - Uses: GetInventoryStatus + GetDailySummary + GetSalesByCategory + GetTotalRevenue

#### Operational Optimization

- _"Identify opportunities to optimize inventory levels based on sales velocity, current stock, and profitability"_

  - Uses: GetInventoryStatus + GetSalesData + GetDetailedInventory + GetSalesByCategory

- _"What products should I promote this week based on high inventory, low recent sales, and category performance?"_
  - Uses: GetInventoryStatus + GetSalesData + GetSalesByCategory + GetDailySummary

### 🎯 **Real-time Operational Questions**

#### Daily Operations

- _"What critical issues need my attention right now?"_
- _"Show me out-of-stock items and their sales history"_
- _"What categories are underperforming and why?"_
- _"Calculate potential revenue loss from current stock-outs"_

#### Supply Chain Management

- _"Based on current inventory and sales trends, what will stock out in the next week?"_
- _"Which supplier's products are performing best in terms of inventory turnover?"_
- _"Show me products with unusual sales spikes that might deplete inventory quickly"_

### 🔍 **Analytical Deep Dives**

#### Trend Analysis

- _"Compare this week's performance with the same period last month across all metrics"_
- _"What's our inventory turnover rate by category and how does it correlate with profitability?"_
- _"Identify seasonal patterns in our sales and inventory data"_

#### Exception Management

- _"Alert me to any unusual patterns: products with rapid stock depletion, categories with declining sales, or inventory imbalances"_
- _"Show me products where actual sales significantly differ from expected patterns"_
- _"Identify products that have been sitting in inventory longest without recent sales"_

### 💡 **Advanced Business Scenarios**

#### Financial Planning

- _"Calculate our current inventory value, today's revenue, and projected losses from stock-outs"_
- _"Show me the financial impact of our current inventory decisions"_
- _"What's our most profitable product mix based on inventory investment and sales performance?"_

#### Competitive Analysis

- _"Which product categories drive the most revenue and how are their inventory levels?"_
- _"What's our market positioning based on product availability and sales velocity?"_
- _"Identify products where we're missing sales opportunities due to stock issues"_

### 🚀 **Multi-dimensional Analysis**

Claude can combine all tools to answer complex questions like:

- _"Create a comprehensive report showing: current inventory health, today's sales performance, products requiring immediate action, revenue analysis by category, and recommendations for tomorrow's operations"_

- _"Analyze our supermarket's operational efficiency: inventory turnover, sales trends, stock availability, category performance, and identify three key areas for improvement"_

- _"Prepare a business intelligence summary for the weekly management meeting covering: performance metrics, inventory status, category analysis, and strategic recommendations"_

### 🔮 **Phase 3: Predictive Analytics Questions**

**Demand Forecasting:**

- _"What products will likely run out of stock in the next two weeks?"_
- _"Predict tomorrow's demand for dairy products with confidence levels"_
- _"Show me demand forecasts for all products with their recommended stock levels"_

**Risk Analysis:**

- _"Which products have the highest stockout risk and what's the potential revenue impact?"_
- _"Alert me to critical inventory situations requiring immediate action"_
- _"Analyze stockout risks for the next month and categorize by priority"_

**Seasonal Intelligence:**

- _"What seasonal patterns exist in our sales data and how should I prepare?"_
- _"Show me seasonal trends for beverage categories and predict next quarter's performance"_
- _"Compare current sales to seasonal expectations - are we on track?"_

**Smart Reordering:**

- _"Generate intelligent reorder recommendations based on predictive analytics"_
- _"What products need urgent reordering and in what quantities?"_
- _"Create a prioritized reorder plan for the next two weeks"_

**Proactive Management:**

- _"Show me all critical alerts that need my immediate attention"_
- _"Combine demand forecasts with current inventory to identify optimization opportunities"_
- _"What's my risk-adjusted inventory strategy for next month?"_

**Strategic Planning:**

- _"Based on seasonal trends and demand forecasts, how should I adjust inventory levels?"_
- _"Predict which categories will outperform or underperform next quarter"_
- _"Create a data-driven inventory optimization plan with ROI projections"_

### 🎯 **Key Advantages**

With the enhanced MCP tools, Claude can:

- **Provide real-time insights** without needing specific date parameters
- **Combine multiple data sources** for comprehensive analysis
- **Identify actionable items** requiring immediate attention
- **Support strategic decision-making** with live business intelligence
- **Generate executive summaries** combining operational and financial metrics
- **Predict potential issues** before they impact operations

These capabilities transform Claude into a **comprehensive business intelligence assistant** for supermarket operations!

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

### 3. Build for Claude Desktop Integration

For the best development experience with Claude Desktop, use the published executable approach:

```bash
# Build and publish the MCP server
.\build-and-publish.ps1

# Or use the batch file
.\build-and-publish.bat
```

This approach:

- ✅ **Eliminates file locking issues** with Claude Desktop
- ✅ **Provides consistent performance** with optimized builds
- ✅ **Automates the build process** with dependency checks
- ✅ **Gracefully handles running processes**

### 4. Claude Desktop Configuration

Use this configuration in your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "supermarket": {
      "command": "D:\\DotNetOpenAI\\McpServer\\publish\\McpServer.exe"
    }
  }
}
```

**Development Workflow:**

1. Make code changes
2. Run `.\build-and-publish.ps1`
3. Restart Claude Desktop (if prompted)
4. Test your changes

> **Note**: See `claude-configs-reference.md` for alternative configuration options.
