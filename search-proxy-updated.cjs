// Please rename this file to search-proxy.cjs and run with: node search-proxy.cjs
const express = require("express");
const axios = require("axios");
require("dotenv").config();

const app = express();
app.use(express.json());

// Add CORS middleware
app.use((req, res, next) => {
  res.header("Access-Control-Allow-Origin", "*");
  res.header("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
  res.header(
    "Access-Control-Allow-Headers",
    "Origin, X-Requested-With, Content-Type, Accept, Authorization"
  );

  if (req.method === "OPTIONS") {
    res.sendStatus(200);
  } else {
    next();
  }
});

// ✅ FIXED: Updated to use correct server URL (port 5000, not 9090)
const MCP_SERVER_URL =
  process.env.VITE_MCP_SERVER_URL || "http://localhost:5000";

// Schema endpoint - proxy to backend MCP server
app.get("/api/tools/schema", async (req, res) => {
  console.log("Received request to /api/tools/schema");

  try {
    console.log("Making request to MCP server for schema...");
    // ✅ FIXED: Updated to use correct schema endpoint
    const mcpRes = await axios.get(
      `${MCP_SERVER_URL}/api/supermarket/tools/schema`
    );
    console.log("MCP server schema response status:", mcpRes.status);
    const data = mcpRes.data;
    console.log("Schema response received, sending back to client");
    res.json(data);
  } catch (err) {
    console.error("Error in schema proxy:", err.message);
    if (err.response) {
      console.error("MCP server error response:", err.response.data);
    }
    res.status(500).json({ error: "Schema proxy error", details: err.message });
  }
});

// MCP Tool Call endpoint - proxy to backend MCP server
app.post("/api/tool", async (req, res) => {
  console.log("Received request to /api/tool");
  console.log("Request body:", req.body);

  const { tool, arguments: args } = req.body;

  try {
    console.log(`Making request to MCP server for tool: ${tool}`);

    // Handle multi_tool_use wrapper
    if (tool === "multi_tool_use") {
      console.log("Handling multi_tool_use request");

      // Handle the new format with query parameter
      if (args && args.query) {
        console.log("Processing multi_tool_use with query:", args.query);

        // Use Azure Search to find the best tool for this query
        const searchResult = await searchForTool(args.query);

        if (searchResult && searchResult.functionName) {
          console.log(
            `Found tool: ${searchResult.functionName} for query: ${args.query}`
          );

          // Extract parameters from the original user input
          const extractedParams = extractParametersFromQuery(
            args.originalUserInput || args.query
          );

          const result = await callSingleTool(
            searchResult.functionName,
            extractedParams
          );

          res.json(result);
          return;
        } else {
          throw new Error(`No suitable tool found for query: ${args.query}`);
        }
      }

      // Handle the original format with tool_uses
      if (args && args.tool_uses) {
        const results = [];

        for (const toolUse of args.tool_uses) {
          const actualTool = toolUse.recipient_name;
          const actualArgs = toolUse.parameters;

          // Extract the actual tool name (remove "functions." prefix if present)
          const cleanToolName = actualTool
            .replace(/^functions\./, "")
            .replace(/^search_azure_cognitive$/, "GetDetailedInventory");

          console.log(
            `Processing tool: ${cleanToolName} with args:`,
            actualArgs
          );

          const result = await callSingleTool(cleanToolName, actualArgs);
          results.push(result);
        }

        res.json({
          tool: "multi_tool_use",
          data: results,
        });
        return;
      }

      throw new Error(
        "multi_tool_use requires either 'query' or 'tool_uses' parameter"
      );
    }

    // Handle single tool calls
    const result = await callSingleTool(tool, args);
    res.json(result);
  } catch (err) {
    console.error("Error in tool proxy:", err.message);
    if (err.response) {
      console.error("MCP server error response:", err.response.data);
    }
    res.status(500).json({ error: "Tool proxy error", details: err.message });
  }
});

// ✅ UPDATED: Enhanced tool endpoint mapping for new plugin architecture
async function callSingleTool(tool, args) {
  console.log(`Calling single tool: ${tool} with args:`, args);

  // ✅ UPDATED: Complete tool mapping for both Supermarket and GkApi plugins
  const toolEndpointMap = {
    // Supermarket Plugin endpoints
    GetProducts: "/api/supermarket/products",
    GetDetailedInventory: "/api/supermarket/inventory/detailed",
    GetInventoryStatus: "/api/supermarket/inventory/status",
    GetLowStockProducts: "/api/supermarket/products/low-stock",
    GetSalesData: "/api/supermarket/sales",
    GetTotalRevenue: "/api/supermarket/revenue",
    GetSalesByCategory: "/api/supermarket/sales/by-category",
    GetDailySummary: "/api/supermarket/sales/daily-summary",

    // ✅ NEW: GkApi Plugin endpoints
    GetPricesWithoutBaseItem: "/api/gkapi/prices-without-base-item",
    GetLatestStatistics: "/api/gkapi/latest-statistics",
    GetContentTypesSummary: "/api/gkapi/content-types",

    // ✅ NEW: Health check endpoints
    CheckSupermarketHealth: "/api/supermarket/health",
    CheckGkApiHealth: "/api/gkapi/health",
    CheckSystemHealth: "/health",

    // Legacy mappings
    search_azure_cognitive: "/api/supermarket/inventory/detailed",
  };

  const endpoint = toolEndpointMap[tool];
  if (!endpoint) {
    throw new Error(
      `Unknown tool: ${tool}. Available tools: ${Object.keys(
        toolEndpointMap
      ).join(", ")}`
    );
  }

  // Build query parameters from arguments
  const queryParams = new URLSearchParams();
  if (args) {
    Object.entries(args).forEach(([key, value]) => {
      if (value !== undefined && value !== null && key !== "query") {
        queryParams.append(key, value.toString());
      }
    });
  }

  const fullUrl = `${MCP_SERVER_URL}${endpoint}${
    queryParams.toString() ? "?" + queryParams.toString() : ""
  }`;
  console.log(`Calling MCP server: ${fullUrl}`);

  const mcpRes = await axios.get(fullUrl);
  console.log("MCP server tool response status:", mcpRes.status);

  return {
    tool,
    data: mcpRes.data,
  };
}

// ✅ ENHANCED: Azure Search endpoint with better error handling
app.post("/api/search", async (req, res) => {
  console.log("Received request to /api/search");
  console.log("Request body:", req.body);

  const { query } = req.body;
  const endpoint = process.env.AZURE_SEARCH_ENDPOINT;
  const apiKey = process.env.AZURE_SEARCH_APIKEY;
  const indexName = process.env.AZURE_SEARCH_INDEX || "mcp-tools"; // ✅ UPDATED: Default to mcp-tools index

  console.log("Azure Search config:", {
    endpoint,
    indexName,
    hasApiKey: !!apiKey,
  });

  try {
    console.log("Making request to Azure Search...");
    const azureRes = await axios.post(
      `${endpoint}/indexes/${indexName}/docs/search?api-version=2021-04-30-Preview`,
      { search: query, top: 10 },
      {
        headers: {
          "Content-Type": "application/json",
          "api-key": apiKey,
        },
      }
    );
    console.log("Azure Search response status:", azureRes.status);
    const data = azureRes.data;

    // Enhanced debugging for both plugin types
    console.log("=== AZURE SEARCH DEBUG ===");
    console.log("Search query:", query);
    console.log("Number of results:", data.value?.length || 0);

    if (data.value && data.value.length > 0) {
      console.log("Available tools found:");
      data.value.forEach((tool, index) => {
        console.log(`\n--- Tool ${index + 1} ---`);
        console.log(
          `Function Name: ${tool.functionName || tool.name || "N/A"}`
        );
        console.log(`Plugin: ${tool.pluginName || "N/A"}`); // ✅ NEW: Plugin name
        console.log(
          `Description: ${tool.description?.substring(0, 150) || "N/A"}...`
        );
        console.log(`Endpoint: ${tool.endpoint || "N/A"}`);
        console.log(`HTTP Method: ${tool.httpMethod || "N/A"}`);
        console.log(`Category: ${tool.category || "N/A"}`);
        console.log(
          `Is Active: ${tool.isActive !== undefined ? tool.isActive : "N/A"}`
        );

        // ✅ NEW: Check for GkApi tools
        const isGkApiTool =
          tool.pluginName?.toLowerCase().includes("gkapi") ||
          tool.functionName?.toLowerCase().includes("gkapi") ||
          tool.endpoint?.includes("/gkapi/");

        if (isGkApiTool) {
          console.log(`*** GKAPI ANALYTICS TOOL DETECTED ***`);
        }

        // Check for category-related tools
        const isCategoryTool =
          (tool.functionName &&
            (tool.functionName.toLowerCase().includes("category") ||
              tool.functionName.toLowerCase().includes("filter") ||
              tool.functionName.toLowerCase().includes("bycategory"))) ||
          (tool.description &&
            (tool.description.toLowerCase().includes("category") ||
              tool.description.toLowerCase().includes("filter by")));

        if (isCategoryTool) {
          console.log(`*** CATEGORY TOOL DETECTED ***`);
        }
      });
    } else {
      console.log("No tools found in Azure Search response!");
    }
    console.log("=== END AZURE SEARCH DEBUG ===");

    res.json(data);
  } catch (err) {
    console.error("Error in Azure Search proxy:", err.message);
    if (err.response) {
      console.error("Azure Search error response:", err.response.data);
    }
    res
      .status(500)
      .json({ error: "Azure Search proxy error", details: err.message });
  }
});

// ✅ ENHANCED: Better tool search with plugin awareness
async function searchForTool(query) {
  console.log(`Searching for tool with query: ${query}`);

  const endpoint = process.env.AZURE_SEARCH_ENDPOINT;
  const apiKey = process.env.AZURE_SEARCH_APIKEY;
  const indexName = process.env.AZURE_SEARCH_INDEX || "mcp-tools"; // ✅ UPDATED

  try {
    const azureRes = await axios.post(
      `${endpoint}/indexes/${indexName}/docs/search?api-version=2021-04-30-Preview`,
      {
        search: query,
        top: 5,
        // ✅ NEW: Enhanced search with plugin filtering
        filter: "isActive eq true", // Only return active tools
        orderby: "pluginName,functionName", // Order by plugin then function
      },
      {
        headers: {
          "Content-Type": "application/json",
          "api-key": apiKey,
        },
      }
    );

    console.log("Azure Search response status:", azureRes.status);
    const searchData = azureRes.data;

    if (searchData.value && searchData.value.length > 0) {
      // ✅ ENHANCED: Prefer GkApi tools for analytics queries
      const isAnalyticsQuery =
        query.toLowerCase().includes("analytics") ||
        query.toLowerCase().includes("statistics") ||
        query.toLowerCase().includes("content") ||
        query.toLowerCase().includes("gk") ||
        query.toLowerCase().includes("mongodb");

      let bestTool;
      if (isAnalyticsQuery) {
        // Prefer GkApi tools for analytics
        bestTool =
          searchData.value.find(
            (tool) =>
              tool.pluginName?.toLowerCase().includes("gkapi") &&
              tool.functionName &&
              tool.endpoint
          ) ||
          searchData.value.find((tool) => tool.functionName && tool.endpoint) ||
          searchData.value[0];
      } else {
        // Default to first valid tool
        bestTool =
          searchData.value.find((tool) => tool.functionName && tool.endpoint) ||
          searchData.value[0];
      }

      console.log(
        `Selected tool: ${
          bestTool.functionName || bestTool.name
        } from plugin: ${bestTool.pluginName || "Unknown"}`
      );
      return bestTool;
    }

    return null;
  } catch (error) {
    console.error("Error searching for tool:", error.message);
    return null;
  }
}

// ✅ ENHANCED: Better parameter extraction with plugin awareness
function extractParametersFromQuery(query) {
  const params = {};
  const lowerQuery = query.toLowerCase();

  // ✅ NEW: Extract parameters for GkApi queries
  if (lowerQuery.includes("statistics") || lowerQuery.includes("summary")) {
    // No special parameters needed for statistics
  }

  if (lowerQuery.includes("content") && lowerQuery.includes("type")) {
    // No special parameters needed for content types
  }

  // Extract date ranges for sales queries
  const dateMatch = lowerQuery.match(/(\d{4}-\d{2}-\d{2})/g);
  if (dateMatch && dateMatch.length >= 1) {
    params.startDate = dateMatch[0];
    if (dateMatch.length >= 2) {
      params.endDate = dateMatch[1];
    }
  }

  // Extract thresholds for low stock
  const thresholdMatch = lowerQuery.match(
    /(?:below|under|less than|threshold)\s*(\d+)/
  );
  if (thresholdMatch) {
    params.threshold = parseInt(thresholdMatch[1]);
  }

  // Extract categories
  const categoryMatch = lowerQuery.match(/category\s*[:=]?\s*([a-zA-Z]+)/);
  if (categoryMatch) {
    params.category = categoryMatch[1];
  }

  console.log(`Extracted parameters for query "${query}":`, params);
  return params;
}

// ✅ NEW: Health check endpoint for the proxy itself
app.get("/health", (req, res) => {
  res.json({
    status: "healthy",
    service: "search-proxy",
    timestamp: new Date().toISOString(),
    mcpServerUrl: MCP_SERVER_URL,
  });
});

const PORT = process.env.PORT || 5002;
app.listen(PORT, () => {
  console.log(`✅ Proxy running on http://localhost:${PORT}`);
  console.log(`🔗 Connecting to MCP Server: ${MCP_SERVER_URL}`);
  console.log(
    `📊 Azure Search Index: ${process.env.AZURE_SEARCH_INDEX || "mcp-tools"}`
  );
});
