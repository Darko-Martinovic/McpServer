# Supermarket MCP Server

A modern, dual-mode server for supermarket inventory and sales management. Supports both **Web API** (REST) and **console/MCP** operation for integration with AI tools like Claude Desktop.

## 🎬 Demo Video

See the McpServer in action with Claude Desktop integration and Web API functionality:

https://github.com/user-attachments/assets/7675de32-fa05-48e1-9327-8417670e9165

_The demo showcases dual-mode operation, MCP tool integration with Claude Desktop, and REST API endpoints._

## 🆕 Latest Features

Recent Additions:

- **API Versioning** - All endpoints now use versioned paths (`/api/v1/...`) for future compatibility
- **Unified Tool Proxy** - Native .NET tool execution service replacing external middleware
- **Articles with Ingredients** - New endpoint for retrieving articles with ingredient information from MongoDB
- **Custom Plugin Support** - Define custom logic to access your database or API through configurable plugins
- **Advanced JSON Viewer** - Interactive data visualizer with expand/collapse functionality and intelligent formatting
- **Enhanced AI Response Tracking** - Each AI response now includes detailed metadata:
  - Number of tokens used (prompt + completion)
  - Estimated cost breakdown
  - Tools utilized during processing
  - AI model information

## Features

- **Dual Mode:** Run as a Web API (REST endpoints) or as a console MCP tool provider
- **API Versioning:** URI-based versioning (`/api/v1/...`) for all endpoints
- **Plugin Architecture:** Extensible system supporting multiple data sources and operations
- **Supermarket Analytics:** Inventory, sales, revenue, category, and stock tools (SQL Server)
- **ThirdApi Integration:** MongoDB-based operations for prices analysis, content statistics, and article ingredients
- **Unified Tool Proxy:** Execute any MCP tool via a single endpoint with automatic routing
- **Resource-like Endpoints:** Real-time inventory, daily summaries, detailed product info
- **Advanced Analytics:** Complex MongoDB aggregation pipelines for data analysis
- **Comprehensive Logging:** Serilog-based, file and console

## Quick Start

### Prerequisites

- .NET 9.0+
- SQL Server 2014+ (for Supermarket plugin)
- MongoDB 4.0+ (for ThirdApi plugin, optional)

### Setup

1. **Database:**
   - Run `Database/SetupDatabase.sql` to create tables and sample data
2. **Configure:**
   - Edit `appsettings.json` with your SQL Server connection string
3. **Build:**
   ```bash
   dotnet build
   ```
4. **Run:**
   - **Web API mode:**
     ```bash
     dotnet run --webapi
     # or use your published executable with --webapi
     ```
   - **Console/MCP mode:**
     ```bash
     dotnet run
     # or use with Claude Desktop/Inspector as shown below
     ```

## Usage

### Web API Mode

- Start the server with `--webapi`
- **Base URL:** `http://localhost:5000`
- **API Version:** `v1`

#### Tool Proxy Endpoints (Unified Tool Execution)

- `POST /api/v1/tool` - Execute any MCP tool by name
- `POST /api/v1/search` - Search for tools via Azure Cognitive Search
- `GET /api/v1/tools/schema` - Get all available tool schemas
- `GET /api/v1/proxy/health` - Tool proxy health check

#### Supermarket Plugin Endpoints (SQL Server)

- `GET /api/v1/supermarket/products` - All products
- `GET /api/v1/supermarket/products/low-stock?threshold=10` - Low stock products
- `GET /api/v1/supermarket/sales?startDate=2025-01-01&endDate=2025-12-31` - Sales data
- `GET /api/v1/supermarket/revenue?startDate=2025-01-01&endDate=2025-12-31` - Total revenue
- `GET /api/v1/supermarket/inventory/status` - Inventory status
- `GET /api/v1/supermarket/inventory/detailed` - Detailed inventory
- `GET /api/v1/supermarket/health` - SQL Server health check

#### ThirdApi Plugin Endpoints (MongoDB)

- `GET /api/v1/thirdapi/prices-without-base-item` - Prices analysis
- `GET /api/v1/thirdapi/latest-statistics` - Processing statistics
- `GET /api/v1/thirdapi/content-types` - Content types summary
- `GET /api/v1/thirdapi/articles/search?name=MASTI` - Find articles by name
- `GET /api/v1/thirdapi/articles/{contentKey}` - Find article by content key
- `GET /api/v1/thirdapi/articles/ingredients` - Articles with ingredients
- `GET /api/v1/thirdapi/plu-data` - PLU data from SAP Fiori
- `GET /api/v1/thirdapi/health` - MongoDB health check

#### Chat Endpoints

- `POST /api/v1/chat/message` - Process chat message with AI
- `GET /api/v1/chat/functions` - Get available AI functions

#### Health & Documentation

- `GET /health` - General system health check
- `GET /swagger` - Swagger UI API documentation

### MCP Tool Provider Mode

- Start the server without `--webapi`
- Integrate with Claude Desktop or MCP Inspector
- Example Claude config (`claude_desktop_config.json`):
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
- Config file location: `C:\Users\<YourUsername>\AppData\Roaming\Claude`

## Configuration

### Database Connections

- **SQL Server (Supermarket Plugin):** Edit `appsettings.json`:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=your-server;Database=your-db;Integrated Security=true;TrustServerCertificate=true;"
    }
  }
  ```
- **MongoDB (ThirdApi Plugin):** Default connection to `mongodb://localhost:27017/ThirdApi`
  - Connects to `ThirdApi` database
  - Uses `Pump` collection for article and price data
  - Uses `Summary` collection for statistics

### Claude Desktop Integration

- **Claude Desktop:** See above for config example and file location

## Plugin Architecture

The server supports a plugin-based architecture allowing multiple data sources:

- **Supermarket Plugin:** SQL Server-based inventory and sales management
- **ThirdApi Plugin:** MongoDB-based price analysis, content statistics, and article ingredients
- **Extensible Design:** Easy to add new plugins for additional data sources

Each plugin provides:

- MCP tools for AI integration
- REST API endpoints for web clients
- Health monitoring and logging
- Independent data service layer

## API Versioning

All API endpoints use URI-based versioning:

```
/api/v1/supermarket/products
/api/v1/thirdapi/articles/ingredients
/api/v1/tool
```

- **Default Version:** v1.0
- **Version Header:** API responses include `api-supported-versions` header
- **Future Versions:** New versions (v2, v3) can be added without breaking existing clients

## Troubleshooting & Logs

- Logs: `Logs/mcpserver.log` (file), console output
- Common issues: Check connection string, database setup, and logs for errors
- For MCP/Claude issues: Ensure config file is correct and restart the app after changes

## Postman Collection

Import the Postman collection and environment from the `postman/` folder:

- `McpServer-Collection.json` - All API endpoints
- `McpServer-Environment.json` - Environment variables (protocol, host, port, api_version)

## 📄 License

MIT License - see LICENSE file for details.

## ⚠️ Disclaimer

This project was developed independently on personal equipment and in personal time.
It is not affiliated with, endorsed by, or derived from the intellectual property of EPAM Systems or any of its clients.
All examples, configurations, and data are generic and intended solely for demonstration and educational purposes.

---

For more details, see the code and comments. Contributions welcome!
