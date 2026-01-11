# Blazor Server UI Migration Plan

## Overview

Migrate the existing React/Vite task-viewer UI (`tools/task-viewer/`) to an embedded Blazor Server UI within the .NET MCP server. This provides a unified .NET architecture with shared DI container and zero HTTP round-trips.

## Decision Rationale

Based on research in `modern-dotnet-web-architecture-guide-2026.md`:

| Requirement | Blazor Server Fit |
|-------------|-------------------|
| Internal tool (task viewer) | ✅ Perfect - designed for LOB apps |
| Single .NET process | ✅ Runs in-process with MCP server |
| Real-time updates | ✅ Built-in SignalR, no extra code |
| Shared DI container | ✅ Direct access to `ITaskStore` |
| Feature parity with React | ✅ MudBlazor DataGrid matches TanStack Table |
| Dynamic UI | ✅ Full interactivity |
| < 500 concurrent users | ✅ Well within capacity (local dev tool) |

**Rejected alternatives:**
- Blazor WASM: 2-5s initial load, can't access ITaskStore directly
- Blazor Auto: Over-engineered for local tool
- HTMX: Less rich UI, can't match React feature parity
- React + .NET API: Two stacks, defeats unified .NET goal

## Conditional Activation

UI should be **opt-in** via environment variable to avoid resource usage when not needed.

```csharp
// Program.cs pattern
var enableUI = Environment.GetEnvironmentVariable("TASK_MANAGER_UI") != "false";

if (enableUI)
{
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddMudServices();
}

// ... later
if (enableUI)
{
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();
    
    // Optional: auto-open browser
    var port = int.Parse(Environment.GetEnvironmentVariable("TASK_MANAGER_UI_PORT") ?? "9998");
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000);
        Process.Start(new ProcessStartInfo($"http://localhost:{port}") { UseShellExecute = true });
    });
}
```

**Environment variables:**
- `TASK_MANAGER_UI`: "true" (default) or "false" to disable
- `TASK_MANAGER_UI_PORT`: Port number (default: 9998)

## Resource Impact

| Mode | Memory Overhead | CPU Overhead |
|------|----------------|--------------|
| UI Disabled | ~0 MB | None |
| UI Enabled, No Browser | ~5-10 MB | Minimal |
| UI Enabled + Active | ~15-30 MB | Per-interaction |

## Technology Stack

- **Blazor Server** (.NET 9)
- **MudBlazor** (component library - free, feature-rich DataGrid)
- **SignalR** (already built into Blazor Server)
- **Shared DI** with existing MCP services (ITaskStore, IMemoryStore, etc.)

## Source Reference: React UI Structure

Location: `tools/task-viewer/src/`

### Component Inventory

| React Component | Purpose | MudBlazor Equivalent |
|-----------------|---------|---------------------|
| `App.jsx` | Main layout, state management | `MainLayout.razor` |
| `NestedTabs.jsx` | Outer/Profile/Inner tab hierarchy | `MudTabs` + `MudDynamicTabs` |
| `TaskTable.jsx` | TanStack Table for tasks | `MudDataGrid` |
| `TaskDetailView.jsx` | Task detail panel | `MudDialog` or side panel |
| `TaskEditView.jsx` | Inline task editing | `MudDataGrid` EditMode |
| `AgentDropdown.jsx` | Agent selector | `MudSelect` |
| `AgentEditor.jsx` | Agent CRUD | `MudForm` |
| `AgentsListView.jsx` | Agent list | `MudTable` |
| `TemplateEditor.jsx` | Template editing | `MudTextField` multiline |
| `TemplateManagement.jsx` | Template list | `MudDataGrid` |
| `HistoryView.jsx` | History list | `MudTimeline` or `MudTable` |
| `GlobalSettingsView.jsx` | Settings form | `MudSwitch`, `MudTextField` |
| `Toast.jsx` / `ToastContainer.jsx` | Notifications | `MudSnackbar` |
| `LanguageSelector.jsx` | i18n dropdown | `MudSelect` |
| `ChatAgent.jsx` | AI chat interface | Custom + `MudTextField` |

### Layout Structure (From JSX Analysis)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  OUTER TABS: 📁 Projects | 📋 Release Notes | ℹ️ Readme | 🎨 Templates  │
│              | 🤖 Sub-Agents (conditional) | ⚙️ Settings               │
├─────────────────────────────────────────────────────────────────────────┤
│  PROFILE TABS (draggable): [Profile A] [Profile B] [+]                  │
├─────────────────────────────────────────────────────────────────────────┤
│  INNER TABS: Tasks | History | Agents | Project Settings                │
├─────────────────────────────────────────────────────────────────────────┤
│  STATS BAR: [Total] [Pending] [In Progress] [Completed]   🔄 [Search]   │
├─────────────────────────────────────────────────────────────────────────┤
│                         TASK TABLE (DataGrid)                           │
│  ┌─────┬──────────────┬────────┬─────────┬──────────┬────────────────┐  │
│  │ #   │ Name         │ Status │ Agent   │ Created  │ Actions        │  │
│  └─────┴──────────────┴────────┴─────────┴──────────┴────────────────┘  │
├─────────────────────────────────────────────────────────────────────────┤
│  MODALS: Add Profile | Edit Profile | Task Detail | Activation Dialog   │
├─────────────────────────────────────────────────────────────────────────┤
│  TOAST NOTIFICATIONS (bottom-right corner)                              │
└─────────────────────────────────────────────────────────────────────────┘
```

### i18n Resources

Location: `tools/task-viewer/src/i18n/locales/`

14 locale files (JSON format):
- en.json, de.json, es.json, fr.json, hi.json, it.json, ja.json, ko.json, pt.json, ru.json, th.json, tr.json, vi.json, zh.json

**Can be reused directly** with `Microsoft.Extensions.Localization` and `IStringLocalizer<T>`.

### Theme/Colors

Location: `tools/task-viewer/src/theme/colors.js`

Convert to MudTheme configuration in `MudThemeProvider`.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    MCP Server Process                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────┐    ┌─────────────────────────────────┐   │
│  │   MCP Protocol   │    │   Blazor Server (Conditional)   │   │
│  │     (stdio)      │    │   if TASK_MANAGER_UI != false   │   │
│  │   ALWAYS RUNS    │    │   - Kestrel on configurable port│   │
│  └────────┬─────────┘    │   - SignalR circuits            │   │
│           │              │   - Razor components            │   │
│           │              └──────────────┬──────────────────┘   │
│           │                             │                      │
│           └──────────────┬──────────────┘                      │
│                          ▼                                     │
│              ┌─────────────────────┐                           │
│              │   Shared Services   │                           │
│              │   - ITaskStore      │                           │
│              │   - IMemoryStore    │                           │
│              │   - IPromptRenderer │                           │
│              └─────────────────────┘                           │
└─────────────────────────────────────────────────────────────────┘
```

## Implementation Plan

### Phase 1: Project Scaffolding
1. Add Blazor Server to existing `Mcp.TaskAndResearch.csproj`
2. Add MudBlazor NuGet package
3. Create conditional startup logic
4. Create `App.razor`, `MainLayout.razor`, `_Imports.razor`

### Phase 2: Core Components
1. `TaskTable.razor` - MudDataGrid with sorting, filtering, pagination
2. `TaskDetailView.razor` - Task detail dialog/panel
3. `ProfileTabs.razor` - MudDynamicTabs with drag-and-drop
4. `OuterTabs.razor` - Main navigation tabs

### Phase 3: Supporting Views
1. `HistoryView.razor` - History timeline/table
2. `AgentsView.razor` - Agent management
3. `TemplatesView.razor` - Template management
4. `SettingsView.razor` - Global settings

### Phase 4: Features
1. Toast notifications via MudSnackbar
2. i18n integration (reuse JSON locale files)
3. Real-time updates via SignalR (task changes)
4. Auto-refresh toggle

### Phase 5: Polish
1. Theme customization
2. Responsive layout
3. Keyboard shortcuts
4. Error handling

## Key MudBlazor Components to Use

```razor
<!-- Tab Structure -->
<MudTabs>
<MudDynamicTabs>

<!-- Data Display -->
<MudDataGrid T="TaskItem" Items="@tasks" Sortable="true" Filterable="true">
<MudTable>

<!-- Forms -->
<MudTextField>
<MudSelect>
<MudSwitch>
<MudForm>

<!-- Layout -->
<MudGrid>
<MudPaper>
<MudCard>

<!-- Feedback -->
<MudSnackbar>
<MudDialog>
<MudChip>
<MudProgressLinear>
```

## Service Integration Pattern

Blazor components inject services directly (no HTTP layer):

```razor
@inject ITaskStore TaskStore
@inject IMemoryStore MemoryStore

@code {
    private ImmutableArray<TaskItem> tasks;
    
    protected override async Task OnInitializedAsync()
    {
        var document = await TaskStore.ReadDocumentAsync();
        tasks = document.Tasks;
    }
}
```

## Client Configuration Examples

### With UI (Claude Desktop)
```json
{
  "env": {
    "DATA_DIR": "./.mcp-tasks",
    "TASK_MANAGER_UI": "true",
    "TASK_MANAGER_UI_PORT": "9998"
  }
}
```

### Without UI (VS Code Copilot)
```json
{
  "env": {
    "DATA_DIR": "./.mcp-tasks",
    "TASK_MANAGER_UI": "false"
  }
}
```

## Coding Standards (MANDATORY)\n\n### 1. SOLID Principles\n- **SRP**: One component = one responsibility (e.g., TasksPage displays tasks, TaskDetailDialog edits one task)\n- **OCP**: Use interfaces for extensibility (INotificationService, ITaskStore)\n- **LSP**: Derived components must be substitutable\n- **ISP**: Small, focused interfaces\n- **DIP**: Inject abstractions, not concrete types\n\n### 2. Functional Programming\n- **Immutability**: Use `record` types, `ImmutableArray<T>`, `readonly` properties\n- **Pure Functions**: Static methods with no side effects for business logic\n- **Avoid Mutable State**: Prefer returning new objects over mutating existing ones\n\n### 3. Cyclomatic Complexity\n- **Maximum**: 5-6 per method\n- **Techniques**: Guard clauses, extract helper methods, use pattern matching\n- **Complex logic**: Move to dedicated service classes\n\n### 4. Code-Behind Pattern (REQUIRED)\n```\nComponentName.razor      <- Markup ONLY, no @code blocks\nComponentName.razor.cs   <- partial class with all logic\n```\n\n### 5. Static Methods\n- Any method NOT accessing instance state MUST be `static`\n- Improves testability and clarifies intent\n\n## Notes\n\n- React UI code is the **specification** - replicate structure and behavior
- Style/colors don't need to match exactly, layout does
- MudBlazor has excellent DataGrid that matches TanStack Table features
- Shared DI container means direct service access, faster than React's HTTP calls
- Each MCP client instance can independently enable/disable UI
