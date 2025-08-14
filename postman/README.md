# McpServer Postman Collection

This directory contains Postman collection and environment files for testing the McpServer REST API endpoints.

## Files Included

### 1. McpServer-Collection.json
The main Postman collection containing all API endpoints organized into logical folders:

- **Products**: Product inventory management endpoints
- **Sales**: Sales data and revenue endpoints  
- **Inventory**: Inventory status and detailed inventory endpoints
- **Tools**: MCP tools schema and indexing endpoints
- **Chat**: AI conversation and chat endpoints

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
# Option 1: Run with default profile (uses launchSettings.json ports)
dotnet run
# Server will be available at: http://localhost:60543 or https://localhost:60542

# Option 2: Run with explicit web argument (uses hardcoded port 9090)
dotnet run -- --web
# Server will be available at: http://localhost:9090

# Option 3: Run from Visual Studio/VS Code
# Uses launchSettings.json configuration (ports 60542/60543)
```

**Port Configuration Note**: The application has multiple port configurations:
- **launchSettings.json**: `http://localhost:60543` and `https://localhost:60542` 
- **Program.cs with --web**: `http://localhost:9090` (hardcoded)
- Choose the appropriate `base_url` environment variable based on how you start the server

### Environment Variables
All requests use environment variables for flexibility:

- `{{base_url}}`: Server base URL (default: `http://localhost:60543` from launchSettings.json)
- `{{base_url_https}}`: HTTPS server URL (`https://localhost:60542`)
- `{{base_url_webapi}}`: Web API mode URL (`http://localhost:9090` when using `--web`)
- `{{start_date}}` / `{{end_date}}`: Date ranges for sales queries
- `{{target_date}}`: Specific date for daily summaries
- `{{low_stock_threshold}}`: Stock level threshold
- `{{category}}` / `{{supplier}}`: Product filtering
- `{{chat_message}}`: Sample message for chat endpoints

**Important**: Switch the `base_url` variable value based on how you start the server:
- Running from IDE or `dotnet run`: Use `http://localhost:60543`
- Running with `dotnet run -- --web`: Use `http://localhost:9090`

### Test Scripts
Each request includes basic test scripts that verify:
- Status code is 200
- Response contains `success` field
- Response includes `timestamp`

### Sample Requests

#### Get All Products
```
GET {{base_url}}/api/supermarket/products
```

#### Get Sales Data for Date Range
```
GET {{base_url}}/api/supermarket/sales?startDate=2025-01-01&endDate=2025-12-31
```

#### Get Low Stock Products
```
GET {{base_url}}/api/supermarket/products/low-stock?threshold=10
```

#### Chat with AI
```
POST {{base_url}}/api/chat/message
Content-Type: application/json

{
  "message": "What products do we have in stock?",
  "history": []
}
```

## API Endpoints Summary

### Products (`/api/supermarket/products`)
- `GET /products` - All products
- `GET /products/low-stock?threshold={n}` - Low stock products
- `GET /products/category/{category}` - Products by category
- `GET /products/supplier/{supplier}` - Products by supplier

### Sales (`/api/supermarket/sales`)
- `GET /sales?startDate={date}&endDate={date}` - Sales data
- `GET /revenue?startDate={date}&endDate={date}` - Total revenue
- `GET /sales/by-category?startDate={date}&endDate={date}` - Category sales
- `GET /sales/daily-summary?date={date}` - Daily summary

### Inventory (`/api/supermarket/inventory`)
- `GET /inventory/status` - Real-time inventory status
- `GET /inventory/detailed` - Detailed inventory (with optional filters)

### Tools (`/api/supermarket/tools`)
- `GET /tools/schema` - MCP tools schema
- `GET /tools/indexed` - Indexed tools from Azure Search

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
1. **Connection refused**: Ensure McpServer is running on the correct port
2. **404 errors**: Verify the base URL and endpoint paths
3. **Date format errors**: Use YYYY-MM-DD format for date parameters
4. **JSON parsing errors**: Ensure Content-Type is set to application/json for POST requests

### Logs
Check the McpServer logs for detailed error information:
- Console output when running the server
- Log files in the `Logs/` directory

## Response Format
All API endpoints return responses in a consistent format:

```json
{
  "success": true,
  "data": [...],
  "count": 42,
  "timestamp": "2025-08-14T10:30:00Z"
}
```

Error responses:
```json
{
  "success": false,
  "error": "Error message",
  "timestamp": "2025-08-14T10:30:00Z"
}
```
