# MCP Task and Research Server

> 🦐 **A .NET port of [mcp-shrimp-task-manager](https://github.com/cjo4m06/mcp-shrimp-task-manager)** — Intelligent task management for AI-powered development.

A powerful Model Context Protocol (MCP) server that provides advanced task management and research capabilities for AI assistants. This server enables structured task planning, dependency management, and guided research workflows — with an optional Blazor Server UI dashboard.

[![NuGet](https://img.shields.io/nuget/v/Mcp.TaskAndResearch)](https://www.nuget.org/packages/Mcp.TaskAndResearch)

---

## 🚀 Quick Start

### 1. Install

```bash
dotnet tool install -g Mcp.TaskAndResearch
```

### 2. Configure VS Code

Create or edit `.vscode/mcp.json` in your project:

```json
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "mcp-task-and-research",
      "env": {
        "DATA_DIR": "${workspaceFolder}/.mcp-tasks",
        "TASK_MANAGER_UI": "true",
        "TASK_MANAGER_UI_AUTO_OPEN": "true"
      }
    }
  }
}
```

> 💡 **Important**: Always set `DATA_DIR` to a project-specific path to keep tasks isolated between projects. Use `${workspaceFolder}` if your environment supports it, or absolute paths otherwise.

### 3. Start Using

1. **Reload VS Code** (Ctrl+Shift+P → "Developer: Reload Window")
2. **Browser opens automatically** to the Tasks UI
3. **Use MCP tools** via Copilot/Claude: `plan_task`, `execute_task`, `verify_task`, etc.

**Update to latest version:**
```bash
dotnet tool update -g Mcp.TaskAndResearch
```

---

## 💡 What is This?

MCP Task and Research Server transforms how AI agents approach software development. Instead of losing context or repeating work, it provides:

- **🧠 Persistent Memory**: Tasks and progress persist across sessions
- **📋 Structured Workflows**: Guided processes for planning, execution, and verification
- **🔄 Smart Decomposition**: Automatically breaks complex tasks into manageable subtasks
- **🎯 Context Preservation**: Never lose your place, even with token limits
- **🔗 Dependency Tracking**: Automatic management of task relationships

---

## ✨ Core Features

### 📋 Task Management

| Feature | Description |
|---------|-------------|
| **Intelligent Planning** | Deep analysis of requirements before implementation |
| **Task Decomposition** | Break down large projects into atomic, testable units |
| **Dependency Resolution** | Automatic resolution by task name or ID |
| **Progress Monitoring** | Real-time status tracking and updates |
| **Verification & Scoring** | Score task completion against criteria |
| **Task History** | Automatic snapshots and backups |

### 🔬 Research Mode

- **Guided Research**: Systematic exploration of technologies and solutions
- **State Management**: Track research progress across sessions
- **Next Steps Planning**: Define and follow research directions

### 📏 Project Rules

- **Project Standards**: Define and maintain coding standards across your project
- **Consistency**: Ensure AI assistants follow your conventions

### 🤔 Thinking Tools

- **Process Thought**: Structured thinking with branching and revision support
- **Chain-of-Thought**: Encourages reflection and style consistency

### 💾 Memory & Data Management

- **Task Snapshots**: Automatic preservation of task state when `clear_all_tasks` runs (completed tasks are stored in `DATA_DIR/memory`)
- **Memory Store**: Dedicated memory management for context storage
- **Automatic Backups**: Completed tasks are written to timestamped JSON under `DATA_DIR/memory` before a clear; pending tasks are dropped during clear
- **Per-Project Isolation**: Set `DATA_DIR` to a project-scoped path (e.g., `.mcp-tasks`) and launch from the workspace root; without `DATA_DIR`, data is stored in the user-level default directory and shared across projects

### 🖥️ Blazor UI Dashboard

When `TASK_MANAGER_UI=true`:
- **Tasks View**: DataGrid with sorting, filtering, search, status indicators
- **Task Details**: Dialog for viewing/editing tasks, dependencies, related files
- **History View**: Browse all task activity including archived tasks
- **Settings View**: Configure preferences, theme, language
- **Dark/Light Theme**: Toggle with persistent preference
- **Keyboard Shortcuts**: `Ctrl+N`, `Ctrl+F`, `Ctrl+S`, `Ctrl+R`, `Esc`, `?`
- **Real-time Updates**: Tasks sync instantly between MCP and UI

---

## 🎯 Common Use Cases

<details>
<summary><b>Feature Development</b></summary>

```
You: "plan task: add user authentication with JWT"
# Agent analyzes codebase, creates subtasks with dependencies

You: "execute task"
# Implements authentication step by step, tracking progress
```
</details>

<details>
<summary><b>Bug Fixing</b></summary>

```
You: "plan task: fix memory leak in data processing"
# Agent researches issue, creates fix plan with verification criteria

You: "execute all pending tasks"
# Executes all fix tasks, verifying each one
```
</details>

<details>
<summary><b>Research & Learning</b></summary>

```
You: "research: compare Blazor vs React for this project"
# Systematic analysis with pros/cons, saved to memory

You: "plan task: migrate component to chosen framework"
# Creates migration plan based on research findings
```
</details>

<details>
<summary><b>Project Setup</b></summary>

```
You: "init project rules"
# Establishes coding standards and conventions

You: "plan task: set up CI/CD pipeline"
# Creates structured plan following your project rules
```
</details>

---

## 🛠️ Available MCP Tools

### Task Management Tools
| Tool | Description |
|------|-------------|
| `plan_task` | Analyze requirements and create a structured task plan |
| `split_tasks` | Break down complex tasks into smaller subtasks |
| `list_tasks` | List all tasks with optional status filter |
| `execute_task` | Execute a specific task by ID |
| `verify_task` | Verify task completion with scoring |
| `update_task` | Update task details, dependencies, or files |
| `delete_task` | Remove a task by ID |
| `clear_all_tasks` | Archive and clear all tasks |
| `query_task` | Search tasks by keyword or ID |
| `get_task_detail` | Get full details for a specific task |

### Research & Thinking Tools
| Tool | Description |
|------|-------------|
| `research_mode` | Enter guided research mode for a topic |
| `process_thought` | Record structured thinking steps |
| `analyze_task` | Deep analysis of task requirements and approach |
| `reflect_task` | Critical review of analysis results for optimization |

### Project & Utility Tools
| Tool | Description |
|------|-------------|
| `init_project_rules` | Initialize project coding standards |
| `get_server_info` | Returns basic server metadata |
| `play_beep` | Play an audible beep notification (Windows only)

---

## 🛠️ Advanced Installation Options

For most users, the Quick Start above is all you need. For advanced scenarios:

### Clone & Build (For Development)

```bash
git clone https://github.com/d-german/mcp-task-and-research.git
cd mcp-task-and-research
dotnet publish src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj -c Release -o ./publish
```

Then configure with the published DLL path instead of the command name.

---

## ⚠️ Data Storage & Project Isolation

**TL;DR**: Always set `DATA_DIR` to a project-specific path to keep tasks isolated between projects.

### Why This Matters

Without `DATA_DIR`, all projects share one global task list, which causes:
- Tasks from different projects mixed together
- Wrong task execution (agent picks tasks from other projects)
- Broken dependencies across unrelated projects

### Understanding Path Resolution

MCP clients handle path resolution differently based on their capabilities and how they set the working directory:

**✅ Environments Where Relative Paths Work**
- Some MCP clients set the working directory to the project/workspace folder when spawning servers
- In these environments, relative paths like `.mcp-tasks` work perfectly fine
- The path resolves correctly to `<project-root>/.mcp-tasks`
- **Benefit**: Simple, portable configuration across projects

```json
{
  "env": {
    "DATA_DIR": ".mcp-tasks"
  }
}
```

**✅ Environments with Variable Substitution** (e.g., VS Code)
- Support variables like `${workspaceFolder}` in configuration
- Variables are resolved to absolute paths before being passed to the MCP server
- **Benefit**: Explicit control with portability

```json
{
  "env": {
    "DATA_DIR": "${workspaceFolder}/.mcp-tasks"
  }
}
```

**⚠️ Environments Where Relative Paths Fail**
- Some clients don't set a working directory when spawning MCP server processes
- Relative paths resolve based on where the host application was launched (unpredictable)
- **On Windows**: May resolve to `C:\Windows\System32\.mcp-tasks`
- **On macOS/Linux**: May resolve to `/` or other system directories
- **Required**: Use absolute paths

```json
{
  "env": {
    "DATA_DIR": "/Users/username/myproject/.mcp-tasks"
  }
}
```

### Local vs Global Configuration

**Local Configuration Support** (e.g., VS Code with `.vscode/mcp.json`)
- Some environments allow per-project MCP configuration files
- Configuration overrides global settings for that specific project
- Enables project-specific paths and environment variables

**Global Configuration Only**
- Other environments only honor global configuration files
- All projects use the same MCP server configuration
- **Must use absolute paths** specific to each project if you want isolation

💡 **How to determine what works in your environment:**
1. Start with a simple relative path: `"DATA_DIR": ".mcp-tasks"`
2. Create a task and check where the `.mcp-tasks` folder was created
3. If it appears in your project root: ✅ Relative paths work
4. If it appears elsewhere (system directories): Use `${workspaceFolder}/.mcp-tasks` or absolute paths

⚠️ **Check your MCP client's documentation for:**
- Variable substitution support (e.g., `${workspaceFolder}`)
- Local configuration file support (e.g., `.vscode/mcp.json`)
- Working directory behavior for spawned processes

### Best Practices

**Priority order (try these in order):**

1. **Simple relative path** (if your client supports proper working directory):
```json
{
  "env": {
    "DATA_DIR": ".mcp-tasks"
  }
}
```

2. **Variable substitution** (if your client supports it):
```json
{
  "env": {
    "DATA_DIR": "${workspaceFolder}/.mcp-tasks"
  }
}
```

3. **Absolute path** (always works, but less portable):
```json
{
  "env": {
    "DATA_DIR": "C:/projects/my-project/.mcp-tasks"
  }
}
```

**General guidelines:**
- Use `.mcp-tasks` folder convention for consistency
- Add to `.gitignore` for private tasks (or commit for team sharing)
- One `DATA_DIR` per project ensures complete isolation
- Test your configuration by checking where files are actually created
- If files appear in system directories, escalate to option 2 or 3

---

## Environment Variables

### Core Settings
| Variable | Default | Description |
|----------|---------|-------------|
| `DATA_DIR` | `.mcp-tasks` | Folder for task data storage. **Use `${workspaceFolder}/.mcp-tasks` (if supported) or absolute paths for reliability** |
| `TASK_MANAGER_UI` | `false` | Enable Blazor web dashboard (auto-selects available port starting at 9998) |
| `TASK_MANAGER_UI_AUTO_OPEN` | `false` | Auto-open browser when server starts |

### Audio Notification Settings
| Variable | Default | Description |
|----------|---------|-------------|
| `ENABLE_COMPLETION_BEEP` | `false` | Enable audible beep notifications via `play_beep` tool |
| `BEEP_FREQUENCY` | `2500` | Beep frequency in Hz (range: 37-32767) |
| `BEEP_DURATION` | `1000` | Beep duration in milliseconds (range: 100-5000) |

> **Note**: The `play_beep` tool only works on Windows. It's useful for getting notified when an AI agent completes a long-running task.

---

## 📚 History & Task Archival

When you clear the task list (via `clear_all_tasks` MCP tool or UI), completed tasks are automatically saved to snapshot files in the `memory/` folder. The **History View** displays:

- **Active tasks**: Current tasks from `tasks.json`
- **Archived tasks**: Previously cleared tasks from all snapshot files

This means you never lose visibility into past work - cleared tasks remain viewable in History indefinitely.

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
     │ TaskRepository  │ │MemoryRepository│ │  RulesStore   │
     │ (LiteDB/ACID)   │ │ (LiteDB)     │ │  (conventions)│
     └────────┬────────┘ └──────────────┘ └───────────────┘
              │
     ┌────────▼────────┐
     │  Blazor UI      │  (optional, auto-port)
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
- [.NET SDK](https://dotnet.microsoft.com/download/dotnet)
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

## 📄 License

MIT License - See LICENSE file for details.

---

## 🌟 Credits

- **Original Concept**: [mcp-shrimp-task-manager](https://github.com/cjo4m06/mcp-shrimp-task-manager) by [cjo4m06](https://github.com/cjo4m06)
- **.NET Port**: [d-german](https://github.com/d-german)

---

<p align="center">
  <a href="https://github.com/d-german/mcp-task-and-research">GitHub</a> •
  <a href="https://www.nuget.org/packages/Mcp.TaskAndResearch">NuGet</a> •
  <a href="https://github.com/d-german/mcp-task-and-research/issues">Issues</a>
</p>
