# MCP Task and Research Server

> 🦐 **A .NET port of [mcp-shrimp-task-manager](https://github.com/cjo4m06/mcp-shrimp-task-manager)** — Intelligent task management for AI-powered development.

A powerful Model Context Protocol (MCP) server built with **.NET 9.0** that provides advanced task management and research capabilities for AI assistants. This server enables structured task planning, dependency management, and guided research workflows — with an optional Blazor Server UI dashboard.

[![NuGet](https://img.shields.io/nuget/v/Mcp.TaskAndResearch)](https://www.nuget.org/packages/Mcp.TaskAndResearch)

---

## 🌟 Origin & Motivation

This project is a **complete port** of the original [mcp-shrimp-task-manager](https://github.com/cjo4m06/mcp-shrimp-task-manager) from Node.js/TypeScript to C#/.NET 9.0.

### Why This Port Exists

The original Shrimp Task Manager is an excellent concept, but:

- **⚠️ Maintenance Concerns**: The original repository may have periods of inactivity
- **🔒 Security**: Node.js dependencies can accumulate vulnerabilities over time
- **🏢 Enterprise Needs**: Many organizations prefer .NET for its enterprise support, type safety, and performance
- **🔧 Active Development**: This .NET version is actively maintained and updated

### Comparison

| Aspect | Original (Node.js) | This Port (.NET) |
|--------|-------------------|------------------|
| **Runtime** | Node.js 18+ | .NET 9.0 |
| **Installation** | `npm install` + clone | `dotnet tool install -g` ✨ |
| **Type Safety** | TypeScript | C# (stronger) |
| **Dependencies** | npm ecosystem | NuGet (curated) |
| **Performance** | Good | Excellent (compiled, AOT-ready) |
| **UI** | React Task Viewer | Blazor Server Dashboard |

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
| `research_mode` | Enter guided research mode for a topic |
| `process_thought` | Record structured thinking steps |
| `init_project_rules` | Initialize project coding standards |

---

## 🚀 Installation

### Option 1: Global Tool (Recommended)

Install as a global .NET tool - no cloning required:

```bash
dotnet tool install -g Mcp.TaskAndResearch
```

After installation, run `mcp-task-and-research --help` to see setup instructions:

```bash
mcp-task-and-research --help
```

Then configure VS Code (see [Configuration](#configuration) below).

**Update:**
```bash
dotnet tool update -g Mcp.TaskAndResearch
```

### Option 2: Clone & Build

For development or customization:

```bash
git clone https://github.com/d-german/mcp-task-and-research.git
cd mcp-task-and-research
dotnet publish src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj -c Release -o ./publish
```

### Option 1: Global Tool (Recommended)

Install as a global .NET tool - no cloning required:

```bash
dotnet tool install -g Mcp.TaskAndResearch
```

After installation, run `mcp-task-and-research --help` to see setup instructions:

```bash
mcp-task-and-research --help
```

Then configure VS Code (see [Configuration](#configuration) below).

**Update:**
```bash
dotnet tool update -g Mcp.TaskAndResearch
```

### Option 2: Clone & Build

For development or customization:

```bash
git clone https://github.com/d-german/mcp-task-and-research.git
cd mcp-task-and-research
dotnet publish src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj -c Release -o ./publish
```

---

## Configuration

### Global Tool Configuration

After installing globally, add to `.vscode/mcp.json`. A relative `DATA_DIR` (e.g., `.mcp-tasks`) keeps data per project when you start the server from the workspace root; omit `DATA_DIR` to use the user-level default directory (shared across projects).

```json
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "mcp-task-and-research",
      "env": {
        "DATA_DIR": ".mcp-tasks",
        "TASK_MANAGER_UI": "true",
        "TASK_MANAGER_UI_AUTO_OPEN": "true"
      }
    }
  }
}
```

### Clone & Build Configuration

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
        "TASK_MANAGER_UI_AUTO_OPEN": "true"
      }
    }
  }
}
```

---

## ⚠️ Data Storage: Critical Configuration

### Understanding DATA_DIR

The `DATA_DIR` environment variable controls where your tasks are stored. **This is the most important configuration setting** when working with multiple projects.

| Configuration | Storage Location | Use Case |
|--------------|------------------|----------|
| `DATA_DIR` not set | `%LOCALAPPDATA%\mcp-task-and-research` (Windows) or `~/.mcp-task-and-research` (Linux/Mac) | Single project, quick testing |
| `DATA_DIR` set | Your specified path | **Multi-project work (RECOMMENDED)** |

### 🚨 WARNING: Task Confusion Without Project Isolation

**If you work on multiple projects WITHOUT setting `DATA_DIR`, you WILL encounter problems:**

1. **Agent Sees All Tasks**: The AI agent sees tasks from ALL your projects mixed together
2. **Wrong Task Execution**: "Execute the login task" could match a task from a different project
3. **Broken Dependencies**: Task dependencies may reference tasks from other projects
4. **Invalid File References**: Related files point to paths in other projects
5. **Context Pollution**: Agent's understanding gets polluted with irrelevant tasks

**Example of what goes wrong:**

```
You're working on Project-A (e-commerce site)
But your task list contains:
  - "Implement login page" (from Project-B, a blog)
  - "Add shopping cart" (from Project-A) ← This is yours
  - "Fix navigation menu" (from Project-C, a dashboard)

Agent: "I see the login task depends on the navigation menu task..."
       (Wrong! These are from different projects!)
```

### ✅ Recommended: Project-Isolated Configuration

**Always set `DATA_DIR` to a project-specific location:**

```json
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "mcp-task-and-research",
      "env": {
        "DATA_DIR": "C:/projects/my-ecommerce-app/.mcp-tasks",
        "TASK_MANAGER_UI": "true"
      }
    }
  }
}
```

### When Global Storage IS Appropriate

The default global location is fine when:
- ✅ You only work on one project at a time
- ✅ You want to share tasks across all workspaces intentionally
- ✅ You're just experimenting or testing

### Best Practices

1. **One DATA_DIR per project** - Keep tasks isolated
2. **Use `.mcp-tasks` folder convention** - Easy to find and manage
3. **Add to `.gitignore`** - For private tasks: `.mcp-tasks/`
4. **Or commit it** - For shared team tasks (optional)
5. **Use absolute paths** - Avoids working directory issues

### Multi-Project Setup Example

For developers working on multiple projects, configure each project's `.vscode/mcp.json`:

**Project A (E-commerce):**
```json
{
  "env": {
    "DATA_DIR": "C:\\projects\\ecommerce\\.mcp-tasks"
  }
}
```

**Project B (Blog):**
```json
{
  "env": {
    "DATA_DIR": "C:\\projects\\blog\\.mcp-tasks"
  }
}
```

**Project C (Dashboard):**
```json
{
  "env": {
    "DATA_DIR": "C:\\projects\\dashboard\\.mcp-tasks"
  }
}
```

Now each project has completely isolated task management!

---

## Quick Start (5 minutes)

### Step 1: Install

**Global Tool (easiest):**
```bash
dotnet tool install -g Mcp.TaskAndResearch
```

**Or clone & build** (see [Installation](#installation) above).

### Step 2: Configure

Add to your project's `.vscode/mcp.json` (create if it doesn't exist):

```json
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "mcp-task-and-research",
      "env": {
        "DATA_DIR": "C:/your/project/path/.mcp-tasks",
        "TASK_MANAGER_UI": "true",
        "TASK_MANAGER_UI_AUTO_OPEN": "true"
      }
    }
  }
}
```

> ⚠️ **Important**: Always set `DATA_DIR` to your project folder. See [Data Storage](#️-data-storage-critical-configuration) for why this matters.

### Step 3: Start Using

1. **Reload VS Code** or use "Developer: Reload Window" (`Ctrl+Shift+P`)
2. The **browser opens automatically** to the Tasks UI (if `TASK_MANAGER_UI_AUTO_OPEN=true`)
3. **Use MCP tools** via Copilot/Claude: `plan_task`, `execute_task`, `verify_task`, etc.

---

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DATA_DIR` | `.mcp-tasks` | **Use absolute path!** Folder for task data storage |
| `TASK_MANAGER_UI` | `false` | Enable Blazor web dashboard (auto-selects available port starting at 9998) |
| `TASK_MANAGER_UI_AUTO_OPEN` | `false` | Auto-open browser when server starts |

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
     │   TaskStore     │ │ MemoryStore  │ │  RulesStore   │
     │   (tasks)       │ │ (context)    │ │  (conventions)│
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
