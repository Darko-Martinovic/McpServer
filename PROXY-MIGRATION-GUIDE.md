# Node.js Proxy Migration Guide for Plugin Architecture

## 📋 Overview

This document outlines the required changes to your Node.js Express proxy to support the new **dual-plugin architecture** (Supermarket + GkApi plugins) in McpServer.

**Current Status**: Your proxy has been updated to handle the new architecture ✅  
**Location**: `search-proxy-updated.cjs` (rename to `search-proxy.cjs`)

---

## 🔧 Critical Configuration Changes

### 1. **Server URL Correction** ✅ FIXED
```javascript
// ❌ OLD: Incorrect port
const MCP_SERVER_URL = "http://localhost:9090";

// ✅ NEW: Correct port
const MCP_SERVER_URL = process.env.VITE_MCP_SERVER_URL || "http://localhost:5000";
```

### 2. **Environment Variables** ✅ UPDATED
Create `.env` file:
```bash
# McpServer configuration
VITE_MCP_SERVER_URL=http://localhost:5000

# Azure Search configuration (for tool discovery)
AZURE_SEARCH_ENDPOINT=https://your-search-service.search.windows.net
AZURE_SEARCH_APIKEY=your-api-key
AZURE_SEARCH_INDEX=mcp-tools

# Proxy configuration
PORT=5002
```

---

## 🛠️ Required Code Changes

### 1. **Tool Endpoint Mapping** ✅ ENHANCED

The proxy now needs to route to **two different plugins**:

```javascript
const toolEndpointMap = {
  // 🏪 Supermarket Plugin (SQL Server)
  GetProducts: "/api/supermarket/products",
  GetDetailedInventory: "/api/supermarket/inventory/detailed",
  GetInventoryStatus: "/api/supermarket/inventory/status",
  GetLowStockProducts: "/api/supermarket/products/low-stock",
  GetSalesData: "/api/supermarket/sales",
  GetTotalRevenue: "/api/supermarket/revenue",
  GetSalesByCategory: "/api/supermarket/sales/by-category",
  GetDailySummary: "/api/supermarket/sales/daily-summary",

  // 📊 GkApi Plugin (MongoDB Analytics) - NEW!
  GetPricesWithoutBaseItem: "/api/gkapi/prices-without-base-item",
  GetLatestStatistics: "/api/gkapi/latest-statistics",
  GetContentTypesSummary: "/api/gkapi/content-types",

  // 🏥 Health Check endpoints - NEW!
  CheckSupermarketHealth: "/api/supermarket/health",
  CheckGkApiHealth: "/api/gkapi/health",
  CheckSystemHealth: "/health",
};
```

### 2. **Plugin-Aware Tool Search** ✅ ENHANCED

Enhanced Azure Search with plugin preference:

```javascript
async function searchForTool(query) {
  // NEW: Analytics query detection
  const isAnalyticsQuery = 
    query.toLowerCase().includes("analytics") ||
    query.toLowerCase().includes("statistics") ||
    query.toLowerCase().includes("content") ||
    query.toLowerCase().includes("gk") ||
    query.toLowerCase().includes("mongodb");

  // Azure Search with plugin filtering
  const searchResponse = await axios.post(
    `${endpoint}/indexes/${indexName}/docs/search?api-version=2021-04-30-Preview`,
    {
      search: query,
      top: 5,
      filter: "isActive eq true", // Only active tools
      orderby: "pluginName,functionName" // Order by plugin
    }
  );

  // NEW: Prefer GkApi tools for analytics queries
  if (isAnalyticsQuery) {
    const gkapiTool = searchData.value.find(tool => 
      tool.pluginName?.toLowerCase().includes("gkapi")
    );
    return gkapiTool || searchData.value[0];
  }

  return searchData.value[0];
}
```

### 3. **Enhanced Parameter Extraction** ✅ UPDATED

```javascript
function extractParametersFromQuery(query) {
  const params = {};
  const lowerQuery = query.toLowerCase();

  // NEW: GkApi parameter handling
  if (lowerQuery.includes("statistics") || lowerQuery.includes("summary")) {
    // GkApi statistics - no parameters needed
  }

  if (lowerQuery.includes("content") && lowerQuery.includes("type")) {
    // GkApi content types - no parameters needed
  }

  // Existing parameter extraction for Supermarket plugin
  const dateMatch = lowerQuery.match(/(\d{4}-\d{2}-\d{2})/g);
  if (dateMatch && dateMatch.length >= 1) {
    params.startDate = dateMatch[0];
    if (dateMatch.length >= 2) {
      params.endDate = dateMatch[1];
    }
  }

  // Extract thresholds, categories, etc.
  // ... existing logic
  
  return params;
}
```

---

## 📊 New Tool Discovery Features

### 1. **Plugin Detection in Search Results**
```javascript
// Enhanced debugging with plugin awareness
if (data.value && data.value.length > 0) {
  data.value.forEach((tool, index) => {
    console.log(`Function Name: ${tool.functionName}`);
    console.log(`Plugin: ${tool.pluginName}`); // NEW!
    console.log(`Endpoint: ${tool.endpoint}`);
    
    // NEW: GkApi tool detection
    const isGkApiTool = 
      tool.pluginName?.toLowerCase().includes("gkapi") ||
      tool.endpoint?.includes("/gkapi/");
    
    if (isGkApiTool) {
      console.log(`*** GKAPI ANALYTICS TOOL DETECTED ***`);
    }
  });
}
```

### 2. **Health Check Routing**
```javascript
// NEW: Proxy health endpoint
app.get("/health", (req, res) => {
  res.json({
    status: "healthy",
    service: "search-proxy",
    timestamp: new Date().toISOString(),
    mcpServerUrl: MCP_SERVER_URL,
  });
});
```

---

## 🧪 Testing the Updated Proxy

### 1. **Start the Proxy**
```bash
# Install dependencies
npm install express axios dotenv

# Start the proxy
node search-proxy.cjs
```

### 2. **Test Plugin Routing**
```bash
# Test Supermarket plugin
curl -X POST http://localhost:5002/api/tool \
  -H "Content-Type: application/json" \
  -d '{"tool": "GetProducts", "arguments": {"category": "Electronics"}}'

# Test GkApi plugin (NEW!)
curl -X POST http://localhost:5002/api/tool \
  -H "Content-Type: application/json" \
  -d '{"tool": "GetLatestStatistics", "arguments": {}}'

# Test health checks
curl http://localhost:5002/health
```

### 3. **Test Tool Discovery**
```bash
# Test analytics tool discovery
curl -X POST http://localhost:5002/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": "get statistics about content types"}'

# Test inventory tool discovery
curl -X POST http://localhost:5002/api/search \
  -H "Content-Type: application/json" \
  -d '{"query": "show me all products"}'
```

---

## 🚀 React Client Integration

### 1. **Updated API Calls**
```javascript
// Using the proxy for both plugins
const fetchDashboardData = async () => {
  try {
    const [inventory, analytics] = await Promise.all([
      // Supermarket data via proxy
      fetch("/api/tool", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          tool: "GetProducts",
          arguments: {}
        })
      }).then(r => r.json()),
      
      // GkApi data via proxy (NEW!)
      fetch("/api/tool", {
        method: "POST", 
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          tool: "GetLatestStatistics",
          arguments: {}
        })
      }).then(r => r.json())
    ]);

    return {
      products: inventory.data.data,
      analytics: analytics.data.data
    };
  } catch (error) {
    console.error("Failed to fetch dashboard data:", error);
  }
};
```

### 2. **Multi-Tool Search**
```javascript
// Enhanced search with plugin awareness
const searchTools = async (query) => {
  const response = await fetch("/api/search", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ query })
  });
  
  const searchResults = await response.json();
  
  // Results now include plugin information
  return searchResults.value.map(tool => ({
    name: tool.functionName,
    plugin: tool.pluginName, // NEW!
    description: tool.description,
    endpoint: tool.endpoint
  }));
};
```

---

## 🔄 Migration Checklist

### ✅ Completed in Updated Proxy
- [x] Fixed server URL (port 5000)
- [x] Added GkApi tool endpoints
- [x] Enhanced tool search with plugin awareness
- [x] Added health check endpoints
- [x] Updated parameter extraction for GkApi
- [x] Enhanced debugging with plugin detection

### 📝 To Do (Your Action Items)
- [ ] Rename `search-proxy-updated.cjs` to `search-proxy.cjs`
- [ ] Create `.env` file with configuration
- [ ] Install proxy dependencies: `npm install express axios dotenv`
- [ ] Test proxy with both plugins
- [ ] Update React client to use new tool endpoints
- [ ] Deploy proxy to production environment

---

## 🚨 Breaking Changes

### 1. **Tool Names**
Some tools may have new names in the Azure Search index:
```javascript
// Check your Azure Search index for exact tool names
// The proxy maps these to the correct endpoints
```

### 2. **Response Format**
All responses now include plugin metadata:
```javascript
{
  "tool": "GetLatestStatistics",
  "data": {
    "success": true,
    "data": { /* GkApi response */ },
    "totalDocuments": 82056,
    "totalUniqueTypes": 51,
    "timestamp": "2025-10-23T..."
  }
}
```

### 3. **Error Handling**
Enhanced error responses with plugin information:
```javascript
{
  "error": "Tool proxy error",
  "details": "GkApi plugin unavailable",
  "plugin": "gkapi"
}
```

---

## 🆘 Troubleshooting

### Common Issues

1. **"Unknown tool" errors**
   - Check Azure Search index has been updated with new tools
   - Verify tool names in `toolEndpointMap`

2. **Connection refused to port 9090**
   - Ensure `VITE_MCP_SERVER_URL=http://localhost:5000` in `.env`

3. **GkApi tools not found**
   - Verify MongoDB is running and connected
   - Check McpServer is running with both plugins

### Debug Commands
```bash
# Test McpServer directly
curl http://localhost:5000/health
curl http://localhost:5000/api/gkapi/health

# Test proxy routing
curl http://localhost:5002/health
curl -X POST http://localhost:5002/api/tool -d '{"tool":"CheckSystemHealth","arguments":{}}'
```

---

## 📈 Performance Considerations

### Caching Strategy
```javascript
// Consider implementing caching for different plugin types
const CACHE_TTL = {
  supermarket: 30000,  // 30 seconds (real-time inventory)
  gkapi: 300000,       // 5 minutes (analytics data)
  health: 10000        // 10 seconds (health checks)
};
```

### Connection Pooling
```javascript
// For high traffic, consider connection pooling
const axiosInstance = axios.create({
  baseURL: MCP_SERVER_URL,
  timeout: 30000, // 30 second timeout for GkApi queries
  keepAlive: true
});
```

---

**🎉 Your proxy is now ready for the dual-plugin architecture!**

_Last updated: October 23, 2025_