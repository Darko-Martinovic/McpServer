# Claude Desktop MCP Configuration Reference

This file contains different configuration options for your `claude_desktop_config.json` file.

## 🚀 Production Configuration (Recommended)

**Use this for stable development** - Uses published executable, no file locking issues:

```json
{
  "mcpServers": {
    "supermarket": {
      "command": "D:\\DotNetOpenAI\\McpServer\\publish\\McpServer.exe"
    }
  }
}
```

**Workflow:**
1. Make code changes
2. Run `.\build-and-publish.ps1` 
3. Restart Claude Desktop if needed
4. Test your changes

## 🔧 Development Configuration

**Use only for quick testing** - Requires manual process management:

```json
{
  "mcpServers": {
    "supermarket-dev": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "D:\\DotNetOpenAI\\McpServer",
        "--no-build"
      ]
    }
  }
}
```

**Issues with this approach:**
- File locking when Claude Desktop is running
- Need to manually kill processes
- More complex workflow

## 🎯 Recommended Setup

1. **Use the Production Configuration** in your `claude_desktop_config.json`
2. **Run the build script** after code changes: `.\build-and-publish.ps1`
3. **Restart Claude Desktop** only when needed
4. **Test your MCP tools**

This approach eliminates the file locking issues you were experiencing!
