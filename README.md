# Supermarket MCP Server

A .NET 9 server for supermarket inventory, sales and product-catalog data. It runs in one of two modes:

- **MCP mode** (stdio): an MCP tool provider for Claude Desktop, MCP Inspector or any other MCP client
- **Web API mode** (REST): the backend for [react-mcp-client](https://github.com/Darko-Martinovic/react-mcp-client), with Swagger, a unified tool proxy and an Azure OpenAI chat proxy

## 🎬 Demo Video

See the McpServer in action with Claude Desktop integration and Web API functionality:

https://github.com/user-attachments/assets/7675de32-fa05-48e1-9327-8417670e9165

_The demo showcases dual-mode operation, MCP tool integration with Claude Desktop, and REST API endpoints._

## Features

- **Two modes:** MCP over stdio, or a versioned REST API (`/api/v1/...`)
- **Plugin architecture:** each data source is a plugin with its own MCP tools, REST endpoints, data service and health check
- **Supermarket plugin (SQL Server):** products, sales, revenue, categories, inventory, daily summaries, seasonal trends and critical stock alerts
- **Catalog plugin (MongoDB, optional):** article search, prices without an article, import statistics, content types, ingredients and PLU data, built on aggregation pipelines
- **Unified tool proxy:** run any MCP tool by name through one endpoint, and find tools with Azure AI Search
- **Chat:** AI conversations with function calling, and an Azure OpenAI proxy so the React client never holds a key
- **AI response metadata:** tokens used, estimated cost, tools called and model
- **Logging:** Serilog, to the console and `Logs/mcpserver.log`

## Quick Start

### Prerequisites

- .NET 9 SDK
- SQL Server (LocalDB, Express or full) for the Supermarket plugin
- MongoDB 4.0+ for the Catalog plugin (optional)
- Azure OpenAI and Azure AI Search (optional; needed for the chat and tool-search endpoints)

### Setup

1. **Database:** run `Database/SetupDatabase.sql` to create the tables and sample data.
2. **Connection string:** `appsettings.json` points at `(localdb)\MSSQLLocalDB` by default; change `ConnectionStrings:DefaultConnection` if your SQL Server is elsewhere.
3. **Keys:** copy `.env.example` to `.env` and fill in the Azure settings you use. The server reads `.env` from the directory it starts in, and `.env` is never committed.
4. **Build:**
   ```bash
   dotnet build
   ```
5. **Run:**
   ```bash
   # Web API mode (http://localhost:5000, Swagger at /swagger)
   dotnet run -- --webapi

   # MCP mode (stdio)
   dotnet run -- --console
   ```
   `--web`/`--webapi` and `--console`/`--mcp` pick the mode. Pass one explicitly: without it the server guesses from how it was started.

## Configuration

`.env` (see `.env.example`):

| Variable | Used for |
|---|---|
| `AOAI_ENDPOINT`, `AOAI_APIKEY`, `CHATCOMPLETION_DEPLOYMENTNAME` | Azure OpenAI: the chat endpoints and the chat completions proxy |
| `EMBEDDING_DEPLOYMENTNAME` | Read but not used yet |
| `AOAI_API_VERSION` | Optional; the API version for chat completions (default `2024-10-21`) |
| `COGNITIVESEARCH_ENDPOINT`, `COGNITIVESEARCH_APIKEY` | Azure AI Search: tool indexing and `POST /api/v1/search` |
| `CORS_ORIGINS` | Optional; browser origins allowed to call the API (default `http://localhost:5173,http://localhost:5174`) |

`appsettings.json`:

- `ConnectionStrings:DefaultConnection`: the SQL Server database
- `ApplicationMode:Web:Urls`: where the Web API listens (`http://localhost:5000` and `https://localhost:5001`)
- MongoDB: the Catalog plugin connects to `mongodb://localhost:27017`, database `Catalog`, collections `articles` (articles and prices) and `import_stats` (import statistics)

## MCP Mode

Claude Desktop config (`%APPDATA%\Claude\claude_desktop_config.json` on Windows):

```json
{
  "mcpServers": {
    "supermarket": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/McpServer", "--no-build", "--", "--console"]
    }
  }
}
```

Or publish the server (`dotnet publish -c Release -o publish`) and point `command` at `publish/McpServer.exe` with `"args": ["--console"]`, as in `claude-mcp-config.json`. Restart Claude Desktop after a change.

### Tools

| Plugin | Tools |
|---|---|
| Supermarket | `GetProducts`, `GetSalesData`, `GetTotalRevenue`, `GetLowStockProducts`, `GetSalesByCategory`, `GetInventoryStatus`, `GetDailySummary`, `GetDetailedInventory`, `GetSeasonalTrends`, `GetCriticalAlerts`, `PredictDemand`, `GetStockoutRisks`, `GetReorderRecommendations` |
| Catalog | `GetPricesWithoutArticle`, `GetLatestStatistics`, `GetContentTypesSummary`, `FindArticlesByName`, `FindArticleByArticleKey` (keys are zero-padded to 8 digits), `GetPluData`, `GetArticlesWithIngredients` |

## Web API Mode

Base URL `http://localhost:5000`, API version `v1`. Swagger UI at `/swagger` lists every endpoint with its parameters.

#### Tool proxy

- `POST /api/v1/tool` - Run any MCP tool by name
- `POST /api/v1/search` - Find tools with Azure AI Search
- `GET /api/v1/tools/schema` - All tool schemas
- `GET /api/v1/proxy/health` - Tool proxy health

#### Supermarket plugin (SQL Server)

- `GET /api/v1/supermarket/products` - All products
- `GET /api/v1/supermarket/products/low-stock?threshold=10` - Products low in stock
- `GET /api/v1/supermarket/products/category/{category}` - Products in a category
- `GET /api/v1/supermarket/products/supplier/{supplier}` - Products from a supplier
- `GET /api/v1/supermarket/sales?startDate=2025-01-01&endDate=2025-12-31` - Sales
- `GET /api/v1/supermarket/sales/by-category?startDate=...&endDate=...` - Sales by category
- `GET /api/v1/supermarket/sales/daily-summary?date=2025-06-01` - Daily summary (today by default)
- `GET /api/v1/supermarket/revenue?startDate=...&endDate=...` - Total revenue
- `GET /api/v1/supermarket/inventory/status` - Inventory status
- `GET /api/v1/supermarket/inventory/detailed` - Detailed inventory
- `GET /api/v1/supermarket/tools/schema` - The plugin's tool schemas
- `GET /api/v1/supermarket/tools/indexed` - Tools indexed in Azure AI Search

#### Catalog plugin (MongoDB)

- `GET /api/v1/catalog/prices-without-article` - Prices with no matching article
- `GET /api/v1/catalog/latest-statistics` - Latest import statistics
- `GET /api/v1/catalog/content-types` - Content types summary
- `GET /api/v1/catalog/articles/search?name=COLA` - Articles by name
- `GET /api/v1/catalog/articles/{articleKey}` - An article by its key
- `GET /api/v1/catalog/articles/ingredients` - Articles with ingredients
- `GET /api/v1/catalog/plu-data` - PLU data from Acme's back office
- `GET /api/v1/catalog/health` - MongoDB health

#### Chat

- `POST /api/v1/chat/message` - A chat message, answered by Azure OpenAI with the tools
- `POST /api/v1/chat/conversation` - The same, returned as a formatted transcript with the history
- `GET /api/v1/chat/functions` - The functions the AI can call
- `POST /api/v1/chat/completions` - Azure OpenAI chat completion with the server's key, for the React client. The body is passed on as it is, with at most 4000 `max_tokens` and one choice; 503 when Azure OpenAI isn't configured

#### Health

- `GET /health` - Server health

## Plugin Architecture

```
Plugins/
├── Interfaces/   # IToolProvider, IDataService, IPluginMetadata
├── Services/     # Plugin discovery, MCP and controller registration
└── Catalog/      # The Catalog plugin: MCP tools, controller, models, data service
```

The Supermarket plugin's tools are in `SupermarketMcpTools.cs`, its controller in `Controllers/` and its data service in `Services/`. A new data source is a new plugin with its own tools, endpoints and health check.

## Postman Collection

Import both files from `postman/`:

- `McpServer-Collection.json` - Every endpoint
- `McpServer-Environment.json` - `protocol`, `host`, `port`, `api_version`

## Troubleshooting

- **Logs:** `Logs/mcpserver.log` and the console. In MCP mode the console output goes to stderr, so it doesn't break JSON-RPC on stdout.
- **Database errors:** check the connection string and that `SetupDatabase.sql` ran.
- **Chat returns 503:** `.env` is missing the Azure OpenAI settings, or the server started in another directory and didn't find `.env`.
- **The React client gets CORS errors:** run it through Vite's proxy (`npm run dev`), or add its origin to `CORS_ORIGINS`.
- **Claude Desktop doesn't see the tools:** check the config path and `--console`, then restart Claude Desktop.

`search-proxy-complete.cjs` is the earlier Node.js tool proxy, kept for reference; the server's own tool proxy replaces it.

## 📄 License

MIT License - see LICENSE file for details.

## ⚠️ Disclaimer

This project was developed independently on personal equipment and in personal time.
It is not affiliated with, endorsed by, or derived from the intellectual property of any employer or client.
All examples, configurations, and data are generic and intended solely for demonstration and educational purposes.
