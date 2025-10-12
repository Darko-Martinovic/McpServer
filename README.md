# Supermarket MCP Server

A modern, dual-mode server for supermarket inventory and sales management. Supports both **Web API** (REST) and **console/MCP** operation for integration with AI tools like Claude Desktop.

## 🎬 Demo Video

See the McpServer in action with Claude Desktop integration and Web API functionality:

https://github.com/user-attachments/assets/2dc085e2-bed7-4ffd-9f7f-efada64ba9db

_The demo showcases dual-mode operation, MCP tool integration with Claude Desktop, and REST API endpoints._

## Features

- **Dual Mode:** Run as a Web API (REST endpoints) or as a console MCP tool provider
- **Product & Sales Analytics:** Inventory, sales, revenue, category, and stock tools
- **Resource-like Endpoints:** Real-time inventory, daily summaries, detailed product info
- **SQL Server Integration:** Robust, parameterized queries
- **Comprehensive Logging:** Serilog-based, file and console

## Quick Start

### Prerequisites

- .NET 9.0+
- SQL Server 2014+
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
- Access endpoints like:
  - `GET http://localhost:5000/api/Supermarket/products` (all products)
  - `GET http://localhost:5000/api/Supermarket/sales?startDate=2025-01-01&endDate=2025-01-31` (sales data)
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

- **Connection String:** Edit `appsettings.json`:
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=your-server;Database=your-db;Integrated Security=true;TrustServerCertificate=true;"
    }
  }
  ```
- **Claude Desktop:** See above for config example and file location

## Troubleshooting & Logs

- Logs: `Logs/mcpserver.log` (file), console output
- Common issues: Check connection string, database setup, and logs for errors
- For MCP/Claude issues: Ensure config file is correct and restart the app after changes

## 📄 License

MIT License - see LICENSE file for details.

---

For more details, see the code and comments. Contributions welcome!
