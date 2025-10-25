# McpServer Postman Collection

This directory contains Postman collection and environment files for testing the McpServer REST API endpoints with **dual-plugin architecture** (Supermarket + GkApi).

## Files Included

### 1. McpServer-Collection.json

The main Postman collection containing all API endpoints organized into logical folders:

- **Products**: Product inventory management endpoints (Supermarket Plugin)
- **Sales**: Sales data and revenue endpoints (Supermarket Plugin)
- **Inventory**: Inventory status and detailed inventory endpoints (Supermarket Plugin)
- **Tools**: MCP tools schema and indexing endpoints
- **Chat**: AI conversation and chat endpoints
- **GkApi Analytics (MongoDB)**: Advanced analytics and data mining endpoints (GkApi Plugin) ✨ NEW
- **Health Checks**: System and plugin health monitoring endpoints ✨ NEW

### 2. Environment Files

#### McpServer-Development.postman_environment.json

Development environment configuration:

- Base URL: `http://localhost:9090`
- Pre-configured sample values for testing
- Suitable for local development

#### McpServer-Production.postman_environment.json

Production environment configuration:

- Base URL: `https://your-production-domain.com` (update as needed)
- Includes API key variable for authentication
- Production-ready configuration

## Import Instructions

### 1. Import Collection

1. Open Postman
2. Click "Import" button
3. Select `McpServer-Collection.json`
4. The collection will appear in your Collections tab

### 2. Import Environment

1. Click the gear icon (⚙️) in the top right corner
2. Select "Import"
3. Choose either environment file:
   - `McpServer-Development.postman_environment.json` for local testing
   - `McpServer-Production.postman_environment.json` for production
4. Select the imported environment from the dropdown

## Usage

### Starting the Server

Before testing, ensure the McpServer is running in Web API mode:

```bash
# Web API mode (primary, production-ready)
dotnet run -- --webapi
# Server will be available at: http://localhost:5000

# MCP Console mode (demo only, for Claude Desktop integration)
dotnet run --no-launch-profile -- --console
```

**Port Configuration**: The application runs on **port 5000** in Web API mode.

### Environment Variables

All requests use environment variables for flexibility:

- `{{protocol}}`: Protocol (default: `http`)
- `{{host}}`: Server host (default: `localhost`)
- `{{port}}`: Server port (default: `5000`)
- `{{start_date}}` / `{{end_date}}`: Date ranges for sales queries
- `{{target_date}}`: Specific date for daily summaries
- `{{low_stock_threshold}}`: Stock level threshold (default: `10`)
- `{{category}}` / `{{supplier}}`: Product filtering
- `{{chat_message}}`: Sample message for chat endpoints

**Base URL**: All requests construct URLs as `{{protocol}}://{{host}}:{{port}}/api/...`

### Test Scripts

Each request includes basic test scripts that verify:

- Status code is 200
- Response contains `success` field
- Response includes `timestamp`

### Sample Requests

#### Get All Products

```
GET {{protocol}}://{{host}}:{{port}}/api/supermarket/products
```

#### Get Sales Data for Date Range

```
GET {{protocol}}://{{host}}:{{port}}/api/supermarket/sales?startDate=2025-01-01&endDate=2025-12-31
```

#### Get Low Stock Products

```
GET {{protocol}}://{{host}}:{{port}}/api/supermarket/products/low-stock?threshold=10
```

#### GkApi - Get Latest Statistics ✨ NEW

```
GET {{protocol}}://{{host}}:{{port}}/api/gkapi/latest-statistics
```

#### GkApi - Get Content Types ✨ NEW

```
GET {{protocol}}://{{host}}:{{port}}/api/gkapi/content-types
```

#### Health Check ✨ NEW

```
GET {{protocol}}://{{host}}:{{port}}/health
```

#### Chat with AI

```
POST {{protocol}}://{{host}}:{{port}}/api/chat/message
Content-Type: application/json

{
  "message": "What products do we have in stock?",
  "history": []
}
```

## API Endpoints Summary

### Supermarket Plugin (`/api/supermarket`) - SQL Server

#### Products

- `GET /products` - All products
- `GET /products/low-stock?threshold={n}` - Low stock products
- `GET /products/category/{category}` - Products by category
- `GET /products/supplier/{supplier}` - Products by supplier

#### Sales

- `GET /sales?startDate={date}&endDate={date}` - Sales data
- `GET /revenue?startDate={date}&endDate={date}` - Total revenue
- `GET /sales/by-category?startDate={date}&endDate={date}` - Category sales
- `GET /sales/daily-summary?date={date}` - Daily summary

#### Inventory

- `GET /inventory/status` - Real-time inventory status
- `GET /inventory/detailed` - Detailed inventory (with optional filters)

#### Tools

- `GET /tools/schema` - MCP tools schema
- `GET /tools/indexed` - Indexed tools from Azure Search

#### Health

- `GET /health` - SQL Server connectivity check

### GkApi Plugin (`/api/gkapi`) - MongoDB Analytics ✨ NEW

- `GET /prices-without-base-item` - Price analysis without base items
- `GET /latest-statistics` - Processing statistics (82,056 documents, 51 types)
- `GET /content-types` - Content types distribution and analysis
- `GET /articles/search?name={name}` - Find articles by name (partial, case-insensitive) ✨ NEW
- `GET /articles/{contentKey}` - Find article by content key (auto zero-padded) ✨ NEW
- `GET /health` - MongoDB connectivity check

### System Health (`/health`) ✨ NEW

- `GET /health` - Overall system health status

### Chat (`/api/chat`)

- `POST /message` - Process chat message
- `GET /functions` - Available AI functions
- `POST /conversation` - Formatted conversation

## Customization

### Updating Environment Variables

1. Select your environment in Postman
2. Click the eye icon (👁️) next to the environment name
3. Edit values as needed for your testing scenarios

### Adding Authentication

For production environments:

1. Add authentication headers to requests
2. Use the `api_key` environment variable if needed
3. Update the collection's authorization settings

## Troubleshooting

### Common Issues

1. **Connection refused**: Ensure McpServer is running on port 5000 (`dotnet run -- --webapi`)
2. **404 errors**: Verify the endpoint paths match the plugin structure
3. **Date format errors**: Use YYYY-MM-DD format for date parameters
4. **JSON parsing errors**: Ensure Content-Type is set to application/json for POST requests
5. **GkApi slow responses**: MongoDB aggregation queries may take 2-5 seconds (expected)
6. **Plugin unavailable**: Check individual plugin health endpoints

### Logs

Check the McpServer logs for detailed error information:

- Console output when running the server
- Log files in the `Logs/` directory

## Response Format

All API endpoints return responses in a consistent format:

### Standard Response (Supermarket Plugin)

```json
{
  "success": true,
  "data": [...],
  "count": 42,
  "timestamp": "2025-10-25T10:30:00Z"
}
```

### Enhanced Response (GkApi Plugin) ✨ NEW

```json
{
  "success": true,
  "data": [...],
  "count": 51,
  "totalDocuments": 82056,
  "totalUniqueTypes": 51,
  "timestamp": "2025-10-25T10:30:00Z"
}
```

### Health Check Response ✨ NEW

```json
{
  "success": true,
  "status": "healthy",
  "database": "GkApi",
  "timestamp": "2025-10-25T10:30:00Z"
}
```

Error responses:

```json
{
  "success": false,
  "error": "Error message",
  "timestamp": "2025-10-25T10:30:00Z"
}
```

---

## 🎉 What's New in v2.0

### 🎯 GkApi Plugin (MongoDB Analytics)

- **3 new endpoints** for advanced retail analytics
- **82,056 documents** across **51 content types** from GK Software retail system
- Real-time analysis of pricing data, item shelf assignments, and retail operations

### 🏥 Health Monitoring

- **System-wide health check** at `/health`
- **Plugin-specific health checks** for SQL Server and MongoDB
- Monitor database connectivity and plugin availability in real-time

### 📊 Enhanced Response Format

- GkApi responses include `totalDocuments` and `totalUniqueTypes` metadata
- Consistent timestamp format across all endpoints
- Better debugging and monitoring information

### 🔌 Dual-Plugin Architecture

- **Supermarket Plugin**: Traditional retail operations (SQL Server)
- **GkApi Plugin**: Advanced analytics and data mining (MongoDB)
- Independent health monitoring per plugin
