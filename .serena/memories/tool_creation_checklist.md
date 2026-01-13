# Tool Creation Checklist

## CRITICAL: Required Attributes for MCP Tools

When creating a new MCP tool in this project, you **MUST** add both attributes:

### 1. Class-Level Attribute (REQUIRED)
```csharp
[McpServerToolType]
public static class YourNewTools
```

### 2. Method-Level Attribute (REQUIRED)
```csharp
[McpServerTool(Name = "your_tool_name")]
[Description("Description of what the tool does.")]
public static string YourToolMethod(...)
```

## Why This Matters

The MCP server uses `WithToolsFromAssembly()` to discover tools. This method **only scans for classes decorated with `[McpServerToolType]`**. Without the class-level attribute:
- The tool class is never discovered
- The tool will not be registered
- The tool count will be incorrect
- Users will not see the tool in their MCP client

## Historical Issue

This mistake has occurred multiple times:
- **January 2026**: `NotificationTools.cs` was missing `[McpServerToolType]`, causing `play_beep` tool to not appear (16 tools instead of 17)

## Template for New Tool Files

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mcp.TaskAndResearch.Tools.YourCategory;

/// <summary>
/// MCP tools for [description].
/// </summary>
[McpServerToolType]  // <-- DO NOT FORGET THIS!
public static class YourCategoryTools
{
    [McpServerTool(Name = "your_tool_name")]
    [Description("Description for MCP clients.")]
    public static string YourMethod(/* dependencies injected here */)
    {
        // Implementation
    }
}
```

## Verification After Adding New Tools

1. Build the project: `dotnet build`
2. Run tests: `dotnet test`
3. Verify tool count matches expected number
4. Test the tool locally before publishing
