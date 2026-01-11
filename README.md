# MCP Task and Research Server

A Model Context Protocol (MCP) server for task management and research workflows, with an optional Blazor Server UI dashboard.

---

## Quick Start (5 minutes)

### Step 1: Clone & Publish

```bash
git clone https://github.com/your-repo/mcp-task-and-research.git
cd mcp-task-and-research
dotnet publish src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj -c Release -o ./publish
```

### Step 2: Add VS Code MCP Configuration

Create or edit `.vscode/mcp.json` in **any project** where you want to use the task manager:

```json
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "C:/path/to/mcp-task-and-research/publish/Mcp.TaskAndResearch.dll"
      ],
      "env": {
        "DATA_DIR": "C:/path/to/your-project/.mcp-tasks",
        "TASK_MANAGER_UI": "true",
        "TASK_MANAGER_UI_PORT": "9998",
        "TASK_MANAGER_UI_AUTO_OPEN": "true"
      }
    }
  }
}
```

> **Important**: Use **absolute paths** for both the DLL and `DATA_DIR` to ensure the server works correctly regardless of the working directory.

#### Example Configuration (Windows)

```json
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "C:\\dg-task manager\\mcp-task-and-research\\publish\\Mcp.TaskAndResearch.dll"
      ],
      "env": {
        "DATA_DIR": "C:\\projects\\my-project\\.mcp-tasks",
        "TASK_MANAGER_UI": "true",
        "TASK_MANAGER_UI_PORT": "9998",
        "TASK_MANAGER_UI_AUTO_OPEN": "true"
      }
    }
  }
}
```

### Step 3: Start Using

1. **Reload VS Code** or use "Developer: Reload Window" (`Ctrl+Shift+P`)
2. The **browser opens automatically** to the Tasks UI (if `TASK_MANAGER_UI_AUTO_OPEN=true`)
3. **Use MCP tools** via Copilot/Claude: `plan_task`, `execute_task`, `verify_task`, etc.

---

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DATA_DIR` | `.mcp-tasks` | **Use absolute path!** Folder for task data storage |
| `TASK_MANAGER_UI` | `false` | Enable Blazor web dashboard |
| `TASK_MANAGER_UI_PORT` | `9998` | Starting port for web UI (auto-increments if busy) |
| `TASK_MANAGER_UI_AUTO_OPEN` | `false` | Auto-open browser when server starts |

### Multi-Instance Support

Each VS Code window can run its own MCP server instance. The server **automatically finds the next available port** if the configured port is busy:
- First instance: `9998`
- Second instance: `9999`
- Third instance: `10000`
- etc.

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
- **History View**: Browse all task activity including archived tasks (see below)
- **Settings View**: Configure preferences, theme, language

#### History & Task Archival

When you clear the task list (via `clear_all_tasks` MCP tool or UI), completed tasks are automatically saved to snapshot files in the `memories/` folder. The **History View** displays:

- **Active tasks**: Current tasks from `tasks.json`
- **Archived tasks**: Previously cleared tasks from all snapshot files

This means you never lose visibility into past work - cleared tasks remain viewable in History indefinitely.

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
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- VS Code with GitHub Copilot or Claude extension

### Build from Source
```bash
# Clone the repository
git clone https://github.com/your-repo/mcp-task-and-research.git
cd mcp-task-and-research

# Build (debug)
dotnet build

# Run tests
dotnet test

# Publish release build
dotnet publish src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj -c Release -o ./publish
```

### Rebuilding After Updates

When you pull updates from the repository:

```bash
cd mcp-task-and-research
git pull
dotnet publish src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj -c Release -o ./publish
```

> **Note**: You must **stop the MCP server** before republishing (reload VS Code or close it), otherwise the DLL will be locked.

### Run Standalone (Development)

For development without an MCP client:

```bash
# MCP server only (no UI)
dotnet run --project src/Mcp.TaskAndResearch

# With UI enabled
set TASK_MANAGER_UI=true && dotnet run --project src/Mcp.TaskAndResearch
# Then open http://localhost:9998
```

### Using `dotnet run` vs Published DLL

| Method | Command | Best For |
|--------|---------|----------|
| `dotnet run` | `dotnet run --project path/to/csproj` | Development, debugging |
| Published DLL | `dotnet path/to/Mcp.TaskAndResearch.dll` | Production, multi-project use |

For **production use**, always use the published DLL - it's faster to start and doesn't require recompilation.

---

## Known Limitations

1. **JSON File Storage**: Tasks are persisted to `DATA_DIR/tasks.json` - changes from both UI and MCP tools are saved immediately
2. **Single Project**: Currently reads from one `DATA_DIR`; no multi-project switching in UI
3. **No Authentication**: UI has no auth; use for local development only
4. **SignalR Reconnection**: Brief stale data possible during connection recovery

---

## License

MIT License - See LICENSE file for details
