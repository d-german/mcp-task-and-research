# MCP Task and Research Server

A Model Context Protocol (MCP) server for task management and research workflows, with an optional Blazor Server UI dashboard.

## Quick Start

### 1. Clone and Build
```bash
git clone https://github.com/your-repo/mcp-task-and-research.git
cd mcp-task-and-research
dotnet build
```

### 2. Configure VS Code MCP

Add to `.vscode/mcp.json` in your project:

```json
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/path/to/mcp-task-and-research/src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj"
      ],
      "env": {
        "DATA_DIR": ".mcp-tasks",
        "TASK_MANAGER_UI": "true",
        "TASK_MANAGER_UI_PORT": "9998"
      }
    }
  }
}
```

### 3. Start Using

1. **Restart VS Code** or reload the MCP server from the MCP panel
2. **Open the UI** at `http://localhost:9998` (if `TASK_MANAGER_UI=true`)
3. **Use MCP tools** via Copilot/Claude: `plan_task`, `execute_task`, `verify_task`, etc.

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DATA_DIR` | `.mcp-tasks` | Folder for task data (relative to workspace) |
| `TASK_MANAGER_UI` | `false` | Enable Blazor web dashboard |
| `TASK_MANAGER_UI_PORT` | `9998` | Port for web UI |

---

## Features

### MCP Tools
- **Task Management**: Create, update, split, verify, and manage tasks with dependencies
- **Research Mode**: In-depth exploration of programming topics
- **Process Thought**: Record structured reasoning steps
- **Project Rules**: Initialize and manage project conventions

### Blazor UI Dashboard
When `TASK_MANAGER_UI=true`:
- **Tasks View**: DataGrid with sorting, filtering, search, status indicators
- **Task Details**: Dialog for viewing/editing tasks, dependencies, related files
- **History View**: Track completed tasks and verification summaries
- **Templates View**: Manage task templates
- **Agents View**: Monitor agent assignments
- **Settings View**: Configure preferences, theme, language

### UI Features
- **Dark/Light Theme**: Toggle with persistent preference
- **Keyboard Shortcuts**: `Ctrl+N` (new), `Ctrl+F` (search), `Ctrl+S` (save), `Ctrl+R` (refresh), `Esc` (close), `?` (help)
- **Responsive Layout**: Mobile-friendly with adaptive navigation
- **Real-time Updates**: Tasks sync instantly between MCP and UI
- **Error Handling**: User-friendly error messages with recovery

---

## Architecture

### How It Works
```
┌─────────────────┐     ┌──────────────────┐
│   MCP Client    │────▶│  MCP Server      │
│ (Copilot/Claude)│     │  (stdio)         │
└─────────────────┘     └────────┬─────────┘
                                 │
                        ┌────────▼─────────┐
                        │   Shared DI      │
                        │   Container      │
                        └────────┬─────────┘
                                 │
              ┌──────────────────┼──────────────────┐
              │                  │                  │
     ┌────────▼────────┐ ┌──────▼───────┐ ┌───────▼───────┐
     │   TaskStore     │ │ MemoryStore  │ │  RulesStore   │
     │   (tasks)       │ │ (context)    │ │  (conventions)│
     └────────┬────────┘ └──────────────┘ └───────────────┘
              │
     ┌────────▼────────┐
     │  Blazor UI      │  (optional, port 9998)
     │  (MudBlazor)    │
     └─────────────────┘
```

- **Shared Services**: UI and MCP share the same data stores via DI
- **Real-time Sync**: Changes via MCP tools appear instantly in UI and vice versa
- **Conditional Hosting**: UI only starts when `TASK_MANAGER_UI=true`

### Project Structure
```
src/Mcp.TaskAndResearch/
├── Config/           # Configuration and path resolution
├── Data/             # TaskStore, MemoryStore, data models
├── Prompts/          # Prompt template builders
├── Server/           # MCP server configuration
├── Tools/            # MCP tool implementations
├── UI/               # Blazor UI (when enabled)
│   ├── Components/   # Razor components
│   ├── Services/     # UI services
│   └── Theme/        # MudBlazor theme
└── wwwroot/          # Static assets
```

---

## Development

### Prerequisites
- .NET 9 SDK
- VS Code with GitHub Copilot or Claude extension

### Build & Test
```bash
dotnet build
dotnet test
```

### Run Standalone (without MCP client)
```bash
# MCP only
dotnet run --project src/Mcp.TaskAndResearch

# With UI
TASK_MANAGER_UI=true dotnet run --project src/Mcp.TaskAndResearch
# Then open http://localhost:9998
```

---

## Known Limitations

1. **In-Memory Storage**: Tasks exist only during server runtime (not persisted to disk)
2. **Single Project**: Currently reads from one `DATA_DIR`; no multi-project switching in UI
3. **No Authentication**: UI has no auth; use for local development only
4. **SignalR Reconnection**: Brief stale data possible during connection recovery

---

## License

MIT License - See LICENSE file for details
