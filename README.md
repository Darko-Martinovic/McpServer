# Supermarket MCP Server

A modern Model Context Protocol (MCP) server for supermarket inventory and sales management with dependency injection and clean architecture.

## Features

- **Product Management**: Get all products in inventory
- **Sales Analytics**: Retrieve sales data for specific date ranges
- **Revenue Tracking**: Calculate total revenue for periods
- **Inventory Monitoring**: Identify products with low stock levels
- **Category Analysis**: Get sales performance by product category

## Prerequisites

- .NET 9.0 or later
- SQL Server 2014 or later
- Node.js (for MCP Inspector testing)

## Setup

1. **Update Connection String**: Edit `appsettings.json` and replace the connection string with your SQL Server details:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-server-name;Database=your-database-name;Integrated Security=true;TrustServerCertificate=true;"
     }
   }
   ```

2. **Database Schema**: Ensure your SQL Server database has the following tables:
   - `Products` (ProductId, ProductName, Category, Price, StockQuantity, Supplier)
   - `Sales` (SaleId, ProductId, Quantity, UnitPrice, TotalAmount, SaleDate)

## Available Tools

### GetProducts
Retrieves all products in the supermarket inventory.

### GetSalesData
Gets sales data for a specific date range.
- Parameters: `startDate` (YYYY-MM-DD), `endDate` (YYYY-MM-DD)

### GetTotalRevenue
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

## Architecture

The application uses:
- **Clean Architecture**: Organized into Models, Services, and Configuration
- **Dependency Injection**: Configured with Microsoft.Extensions.Hosting
- **Configuration**: JSON-based configuration with appsettings.json
- **Data Access**: Microsoft.Data.SqlClient for SQL Server connectivity
- **MCP Protocol**: ModelContextProtocol for AI tool integration
- **Logging**: Console logging with configurable log levels

## Project Structure

```
McpServer/
├── Models/                          # Data models
│   ├── Product.cs                   # Product entity
│   ├── SalesRecord.cs               # Sales record entity
│   └── CategorySales.cs             # Category sales summary
├── Services/                        # Business logic and data access
│   ├── Interfaces/                  # Service interfaces
│   │   └── ISupermarketDataService.cs
│   └── SupermarketDataService.cs    # SQL Server implementation
├── Configuration/                   # Configuration classes
│   └── ConnectionStringOptions.cs   # Database connection options
├── Program.cs                       # Application entry point and DI setup
├── SupermarketMcpTools.cs           # MCP tools definitions
├── appsettings.json                 # Configuration file
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

# Test with MCP Inspector
npx @modelcontextprotocol/inspector dotnet run --project .
```

## Integration with AI Applications

This MCP server can be integrated with AI applications that support the Model Context Protocol, providing them with access to supermarket data through the defined tools. All tools return JSON-formatted data for easy consumption by AI systems.

## Benefits of the New Structure

- **Separation of Concerns**: Models, services, and configuration are clearly separated
- **Maintainability**: Each class has a single responsibility
- **Testability**: Interfaces allow for easy unit testing with mocks
- **Scalability**: Easy to add new models, services, or tools
- **Clean Architecture**: Follows SOLID principles and clean architecture patterns 