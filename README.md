# Supermarket MCP Server

A modern, dual-mode server for supermarket inventory and sales management. Supports both **Web API** (REST) and **console/MCP** operation for integration with AI tools like Claude Desktop.

## 🎬 Demo Video

See the McpServer in action with Claude Desktop integration and Web API functionality:

https://github.com/user-attachments/assets/7675de32-fa05-48e1-9327-8417670e9165

_The demo showcases dual-mode operation, MCP tool integration with Claude Desktop, and REST API endpoints._

## 🆕 Latest Features

Recent Additions:

- **Custom Plugin Support** - Define custom logic to access your database or API through configurable plugins
- **Advanced JSON Viewer** - Interactive data visualizer with expand/collapse functionality and intelligent formatting
- **Enhanced AI Response Tracking** - Each AI response now includes detailed metadata:
  - Number of tokens used (prompt + completion)
  - Estimated cost breakdown
  - Tools utilized during processing
  - AI model information

## Features

- **Dual Mode:** Run as a Web API (REST endpoints) or as a console MCP tool provider
- **Plugin Architecture:** Extensible system supporting multiple data sources and operations
- **Supermarket Analytics:** Inventory, sales, revenue, category, and stock tools (SQL Server)
- **GK API Integration:** MongoDB-based operations for prices analysis and content statistics
- **Resource-like Endpoints:** Real-time inventory, daily summaries, detailed product info
- **Advanced Analytics:** Complex MongoDB aggregation pipelines for data analysis
- **Comprehensive Logging:** Serilog-based, file and console

## Quick Start

### Prerequisites

- .NET 9.0+
- SQL Server 2014+ (for Supermarket plugin)
- MongoDB 4.0+ (for GkApi plugin, optional)
- Node.js (for MCP Inspector, optional)

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
- **Supermarket Plugin Endpoints:**
  - `GET http://localhost:5000/api/supermarket/products` (all products)
  - `GET http://localhost:5000/api/supermarket/sales?startDate=2025-01-01&endDate=2025-01-31` (sales data)
  - `GET http://localhost:5000/health` (general health check)
- **GkApi Plugin Endpoints:**
  - `GET http://localhost:5000/api/gkapi/prices-without-base-item` (prices analysis)
  - `GET http://localhost:5000/api/gkapi/latest-statistics` (processing statistics)
  - `GET http://localhost:5000/api/gkapi/content-types` (content types summary)
  - `GET http://localhost:5000/api/gkapi/health` (MongoDB health check)
- **API Documentation:** `GET http://localhost:5000/swagger` (Swagger UI)
- Any HTTP client or frontend can consume the API

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
- **MongoDB (GkApi Plugin):** Default connection to `mongodb://localhost:27017/GkApi`
  - Connects to `GkApi` database
  - Uses `Pump` collection for price data
  - Uses `Summary` collection for statistics

### Claude Desktop Integration

- **Claude Desktop:** See above for config example and file location

## Plugin Architecture

The server supports a plugin-based architecture allowing multiple data sources:

- **Supermarket Plugin:** SQL Server-based inventory and sales management
- **GkApi Plugin:** MongoDB-based price analysis and content statistics
- **Extensible Design:** Easy to add new plugins for additional data sources

Each plugin provides:

- MCP tools for AI integration
- REST API endpoints for web clients
- Health monitoring and logging
- Independent data service layer

## Troubleshooting & Logs

- Logs: `Logs/mcpserver.log` (file), console output
- Common issues: Check connection string, database setup, and logs for errors
- For MCP/Claude issues: Ensure config file is correct and restart the app after changes

## 📄 License

MIT License - see LICENSE file for details.

## ⚠️ Disclaimer

This project was developed independently on personal equipment and in personal time.
It is not affiliated with, endorsed by, or derived from the intellectual property of EPAM Systems or any of its clients.
All examples, configurations, and data are generic and intended solely for demonstration and educational purposes.

---

For more details, see the code and comments. Contributions welcome!
