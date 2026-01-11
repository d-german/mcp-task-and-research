# MCP Task and Research Server

A Model Context Protocol (MCP) server for task management and research workflows, featuring an optional Blazor Server UI dashboard.

## Features

### MCP Tools
- **Task Management**: Create, update, split, verify, and manage tasks with dependencies
- **Research Mode**: In-depth exploration of programming topics
- **Process Thought**: Record structured reasoning steps
- **Project Rules**: Initialize and manage project conventions

### Blazor UI Dashboard (Optional)
- **Tasks View**: MudDataGrid with sorting, filtering, pagination, and quick search
- **Task Details**: Dialog for viewing and editing task details, dependencies, and related files
- **History View**: Track completed tasks and verification summaries
- **Templates View**: Manage task templates for common workflows
- **Agents View**: Monitor agent assignments and workloads
- **Settings View**: Configure application preferences

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `TASK_MANAGER_UI` | `false` | Enable Blazor Server UI alongside MCP server |
| `TASK_MANAGER_UI_PORT` | `9998` | Port for the Blazor UI web server |

## Quick Start

### MCP-Only Mode (Default)
```bash
dotnet run --project src/Mcp.TaskAndResearch
```

### With UI Dashboard
```bash
# Windows
set TASK_MANAGER_UI=true
set TASK_MANAGER_UI_PORT=9998
dotnet run --project src/Mcp.TaskAndResearch

# PowerShell
$env:TASK_MANAGER_UI = "true"
$env:TASK_MANAGER_UI_PORT = "9998"
dotnet run --project src/Mcp.TaskAndResearch

# Linux/macOS
TASK_MANAGER_UI=true TASK_MANAGER_UI_PORT=9998 dotnet run --project src/Mcp.TaskAndResearch
```

Then open `http://localhost:9998` in your browser.

## Architecture

### Core Components
- **TaskStore**: In-memory task storage with change notification events
- **MemoryStore**: Persistent memory storage for context between sessions
- **RulesStore**: Project-specific rules and conventions

### UI Layer (When Enabled)
- **MudBlazor 8.0**: Material Design component library
- **Code-Behind Pattern**: All components use `.razor` + `.razor.cs` separation
- **Real-time Updates**: SignalR-based automatic UI refresh when tasks change
- **Responsive Design**: Mobile-friendly layout with adaptive navigation

### Shared Services
The UI and MCP server share the same DI container and data stores:
- Tasks created via MCP tools appear instantly in the UI
- UI modifications are immediately available to MCP clients
- Single source of truth for all task data

## UI Features

### Theme Support
- Light and Dark mode toggle
- Persistent preference via localStorage
- Custom color palette for task status indicators

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| `Ctrl+N` | New task |
| `Ctrl+F` | Focus search |
| `Ctrl+S` | Save |
| `Ctrl+R` | Refresh |
| `Esc` | Close dialog |
| `?` | Show help |

### Error Handling
- Global error boundary catches unhandled exceptions
- User-friendly error messages with recovery option
- Errors logged for debugging

### Loading States
- Global loading overlay for async operations
- Inline loading indicators in data grids
- Scoped loading management for nested operations

## Development

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 / VS Code / Rider

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Project Structure
```
src/Mcp.TaskAndResearch/
├── Config/           # Configuration and path resolution
├── Data/             # TaskStore, MemoryStore, data models
├── Prompts/          # Prompt template builders
├── Server/           # MCP server configuration
├── Tools/            # MCP tool implementations
├── UI/               # Blazor UI components
│   ├── Components/
│   │   ├── Dialogs/  # Modal dialogs
│   │   ├── Layout/   # MainLayout, NavMenu
│   │   ├── Pages/    # Route pages
│   │   └── Shared/   # Reusable components
│   ├── Services/     # UI-specific services
│   └── Theme/        # MudBlazor theme configuration
└── wwwroot/          # Static assets (CSS, JS)
```

## MCP Client Configuration

Add to your MCP client configuration (e.g., Claude Desktop):

```json
{
  "mcpServers": {
    "task-and-research": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/src/Mcp.TaskAndResearch"],
      "env": {
        "TASK_MANAGER_UI": "true",
        "TASK_MANAGER_UI_PORT": "9998"
      }
    }
  }
}
```

## Known Limitations

1. **In-Memory Storage**: Tasks are not persisted to disk by default; they exist only during server runtime
2. **Single Instance**: The UI assumes a single server instance; multiple instances would have separate task stores
3. **No Authentication**: The UI has no built-in authentication; suitable for local development only
4. **SignalR Connection**: Real-time updates require stable SignalR connection; reconnection is automatic but may briefly show stale data

## License

MIT License - See LICENSE file for details
