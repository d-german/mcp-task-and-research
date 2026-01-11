# Blazor UI Coding Standards

## Overview

This document defines the coding standards for the Blazor Server UI migration project. All developers must follow these guidelines to ensure consistent, maintainable, and high-quality code.

---

## 1. SOLID Principles

### Single Responsibility Principle (SRP)

Each component should have one and only one reason to change.

**Example - Correct:**
```csharp
// TasksPage.razor.cs - Only responsible for displaying and coordinating task list
public partial class TasksPage : ComponentBase
{
    [Inject] private ITaskStore TaskStore { get; set; } = default!;
    
    private ImmutableArray<TaskItem> _tasks = [];
    
    protected override async Task OnInitializedAsync()
    {
        _tasks = await TaskStore.GetAllTasksAsync();
    }
}

// TaskDetailDialog.razor.cs - Only responsible for editing a single task
public partial class TaskDetailDialog : ComponentBase
{
    [Parameter] public TaskItem Task { get; set; } = default!;
    [Parameter] public EventCallback<TaskItem> OnSave { get; set; }
}
```

**Example - Incorrect:**
```csharp
// DON'T: One component doing everything
public partial class TaskManager : ComponentBase
{
    // Managing list, editing, validation, notifications all in one place
}
```

### Open/Closed Principle (OCP)

Components should be open for extension but closed for modification.

**Example:**
```csharp
// Define interface for extensibility
public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
}

// Implementation can be swapped without modifying consumers
public class MudSnackbarNotificationService : INotificationService
{
    private readonly ISnackbar _snackbar;
    
    public MudSnackbarNotificationService(ISnackbar snackbar) => _snackbar = snackbar;
    
    public void ShowSuccess(string message) => _snackbar.Add(message, Severity.Success);
    public void ShowError(string message) => _snackbar.Add(message, Severity.Error);
    public void ShowWarning(string message) => _snackbar.Add(message, Severity.Warning);
}
```

### Liskov Substitution Principle (LSP)

Derived components must be substitutable for their base types.

**Example:**
```csharp
// Base dialog component
public abstract class BaseDialog : ComponentBase
{
    [CascadingParameter] protected MudDialogInstance MudDialog { get; set; } = default!;
    
    protected virtual void Close() => MudDialog.Close();
    protected virtual void Cancel() => MudDialog.Cancel();
}

// Derived dialogs can be used anywhere BaseDialog is expected
public partial class TaskDetailDialog : BaseDialog { }
public partial class ConfirmationDialog : BaseDialog { }
```

### Interface Segregation Principle (ISP)

Prefer small, focused interfaces over large ones.

**Example - Correct:**
```csharp
public interface ITaskReader
{
    Task<ImmutableArray<TaskItem>> GetAllTasksAsync();
    Task<TaskItem?> GetTaskByIdAsync(string id);
}

public interface ITaskWriter
{
    Task CreateTaskAsync(TaskItem task);
    Task UpdateTaskAsync(TaskItem task);
    Task DeleteTaskAsync(string id);
}

// Consumers only depend on what they need
public partial class TasksPage : ComponentBase
{
    [Inject] private ITaskReader TaskReader { get; set; } = default!; // Read-only access
}
```

### Dependency Inversion Principle (DIP)

Depend on abstractions, not concrete implementations.

**Example:**
```csharp
// CORRECT: Inject interface
public partial class TasksPage : ComponentBase
{
    [Inject] private ITaskStore TaskStore { get; set; } = default!;
    [Inject] private INotificationService Notifications { get; set; } = default!;
}

// INCORRECT: Don't depend on concrete types
public partial class TasksPage : ComponentBase
{
    [Inject] private JsonFileTaskStore TaskStore { get; set; } = default!; // Bad!
}
```

---

## 2. Functional Programming Patterns

### Immutability

Use immutable types to prevent unintended state mutations.

**Example:**
```csharp
// Use records for DTOs and data models
public record TaskItem(
    string Id,
    string Name,
    string Description,
    TaskStatus Status,
    string? Agent,
    ImmutableArray<string> Dependencies,
    DateTimeOffset CreatedAt);

// Use ImmutableArray for collections
private ImmutableArray<TaskItem> _tasks = [];

// Create new instances instead of mutating
private static TaskItem UpdateStatus(TaskItem task, TaskStatus newStatus)
{
    return task with { Status = newStatus };
}
```

### Pure Functions

Functions should have no side effects and return the same output for the same input.

**Example:**
```csharp
// PURE: No side effects, deterministic output
public static class TaskFilters
{
    public static ImmutableArray<TaskItem> FilterByStatus(
        ImmutableArray<TaskItem> tasks, 
        TaskStatus status)
    {
        return [.. tasks.Where(t => t.Status == status)];
    }
    
    public static ImmutableArray<TaskItem> SortByCreatedDate(
        ImmutableArray<TaskItem> tasks, 
        bool descending = true)
    {
        return descending 
            ? [.. tasks.OrderByDescending(t => t.CreatedAt)]
            : [.. tasks.OrderBy(t => t.CreatedAt)];
    }
}

// IMPURE (Avoid when possible): Has side effects
private void UpdateTask(TaskItem task)
{
    _tasks = [.. _tasks.Where(t => t.Id != task.Id), task]; // Modifies state
    StateHasChanged(); // Side effect
}
```

### Prefer Expressions Over Statements

Use expression-bodied members and LINQ.

**Example:**
```csharp
// Expression-bodied members
public int PendingCount => _tasks.Count(t => t.Status == TaskStatus.Pending);
public bool HasTasks => _tasks.Length > 0;

// LINQ over loops
private static ImmutableArray<TaskItem> GetBlockedTasks(
    ImmutableArray<TaskItem> tasks,
    ImmutableArray<string> completedIds)
{
    return [.. tasks.Where(t => 
        t.Dependencies.Any(d => !completedIds.Contains(d)))];
}
```

---

## 3. Cyclomatic Complexity

**Maximum allowed: 5-6 per method**

### Techniques to Reduce Complexity

#### Guard Clauses

**Before (complexity ~8):**
```csharp
private async Task SaveTask()
{
    if (_task != null)
    {
        if (!string.IsNullOrEmpty(_task.Name))
        {
            if (_task.Status != TaskStatus.Completed || CanCompleteTask())
            {
                await TaskStore.UpdateTaskAsync(_task);
                Notifications.ShowSuccess("Task saved");
            }
            else
            {
                Notifications.ShowError("Cannot complete task");
            }
        }
        else
        {
            Notifications.ShowError("Name required");
        }
    }
}
```

**After (complexity ~3):**
```csharp
private async Task SaveTask()
{
    if (_task is null) return;
    
    if (string.IsNullOrEmpty(_task.Name))
    {
        Notifications.ShowError("Name required");
        return;
    }
    
    if (_task.Status == TaskStatus.Completed && !CanCompleteTask())
    {
        Notifications.ShowError("Cannot complete task");
        return;
    }
    
    await TaskStore.UpdateTaskAsync(_task);
    Notifications.ShowSuccess("Task saved");
}
```

#### Extract Helper Methods

**Before:**
```csharp
private void ProcessTasks()
{
    foreach (var task in _tasks)
    {
        if (task.Status == TaskStatus.Pending)
        {
            // 20 lines of pending logic
        }
        else if (task.Status == TaskStatus.InProgress)
        {
            // 20 lines of in-progress logic
        }
        else
        {
            // 20 lines of completed logic
        }
    }
}
```

**After:**
```csharp
private void ProcessTasks()
{
    foreach (var task in _tasks)
    {
        ProcessTask(task);
    }
}

private void ProcessTask(TaskItem task)
{
    var handler = GetTaskHandler(task.Status);
    handler(task);
}

private static Action<TaskItem> GetTaskHandler(TaskStatus status) => status switch
{
    TaskStatus.Pending => ProcessPendingTask,
    TaskStatus.InProgress => ProcessInProgressTask,
    TaskStatus.Completed => ProcessCompletedTask,
    _ => static _ => { }
};
```

#### Pattern Matching

```csharp
// Use switch expressions for multi-branch logic
private static string GetStatusIcon(TaskStatus status) => status switch
{
    TaskStatus.Pending => Icons.Material.Outlined.Schedule,
    TaskStatus.InProgress => Icons.Material.Outlined.PlayArrow,
    TaskStatus.Completed => Icons.Material.Outlined.CheckCircle,
    TaskStatus.Blocked => Icons.Material.Outlined.Block,
    _ => Icons.Material.Outlined.Help
};

private static Color GetStatusColor(TaskStatus status) => status switch
{
    TaskStatus.Pending => Color.Default,
    TaskStatus.InProgress => Color.Info,
    TaskStatus.Completed => Color.Success,
    TaskStatus.Blocked => Color.Error,
    _ => Color.Default
};
```

---

## 4. Code-Behind Pattern (REQUIRED)

**All Blazor components MUST use the code-behind pattern.**

### File Structure

```
Components/
├── Tasks/
│   ├── TasksPage.razor          <- Markup ONLY
│   ├── TasksPage.razor.cs       <- Partial class with all logic
│   ├── TaskDetailDialog.razor
│   ├── TaskDetailDialog.razor.cs
│   └── TaskRow.razor
│       TaskRow.razor.cs
```

### Markup File (*.razor)

Contains ONLY Razor markup. No `@code` blocks.

**TasksPage.razor:**
```razor
@page "/tasks"
@inherits TasksPageBase

<MudDataGrid T="TaskItem" Items="@Tasks" Sortable="true" Filterable="true">
    <Columns>
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.Status" Title="Status" />
        <TemplateColumn Title="Actions">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Edit" 
                               OnClick="@(() => EditTask(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

### Code-Behind File (*.razor.cs)

Contains the partial class with all logic.

**TasksPage.razor.cs:**
```csharp
namespace Mcp.TaskAndResearch.UI.Components.Tasks;

public partial class TasksPage : ComponentBase
{
    [Inject] private ITaskStore TaskStore { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private INotificationService Notifications { get; set; } = default!;
    
    private ImmutableArray<TaskItem> _tasks = [];
    
    protected ImmutableArray<TaskItem> Tasks => _tasks;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadTasksAsync();
    }
    
    private async Task LoadTasksAsync()
    {
        _tasks = await TaskStore.GetAllTasksAsync();
    }
    
    protected async Task EditTask(TaskItem task)
    {
        var parameters = new DialogParameters<TaskDetailDialog>
        {
            { x => x.Task, task }
        };
        
        var dialog = await DialogService.ShowAsync<TaskDetailDialog>("Edit Task", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await LoadTasksAsync();
            Notifications.ShowSuccess("Task updated");
        }
    }
}
```

---

## 5. Static Methods

**Methods that do not access instance state MUST be declared as `static`.**

### Why?

1. **Performance**: No implicit `this` reference
2. **Clarity**: Signals the method is self-contained
3. **Testability**: Can be tested without instantiating the class
4. **Thread Safety**: No shared state to worry about

### Examples

```csharp
public partial class TasksPage : ComponentBase
{
    private ImmutableArray<TaskItem> _tasks = [];
    
    // INSTANCE METHOD: Accesses _tasks field
    private int GetPendingCount()
    {
        return _tasks.Count(t => t.Status == TaskStatus.Pending);
    }
    
    // STATIC METHOD: Does not access any instance state
    private static bool IsTaskBlocked(TaskItem task, ImmutableArray<string> completedIds)
    {
        return task.Dependencies.Any(d => !completedIds.Contains(d));
    }
    
    // STATIC METHOD: Pure transformation
    private static ImmutableArray<TaskItem> SortTasks(
        ImmutableArray<TaskItem> tasks, 
        string sortField, 
        bool ascending)
    {
        return sortField switch
        {
            "name" => ascending 
                ? [.. tasks.OrderBy(t => t.Name)] 
                : [.. tasks.OrderByDescending(t => t.Name)],
            "status" => ascending 
                ? [.. tasks.OrderBy(t => t.Status)] 
                : [.. tasks.OrderByDescending(t => t.Status)],
            "created" => ascending 
                ? [.. tasks.OrderBy(t => t.CreatedAt)] 
                : [.. tasks.OrderByDescending(t => t.CreatedAt)],
            _ => tasks
        };
    }
    
    // STATIC METHOD: Validation logic
    private static ImmutableArray<string> ValidateTask(TaskItem task)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(task.Name))
            errors.Add("Name is required");
            
        if (task.Name?.Length > 200)
            errors.Add("Name must be 200 characters or less");
            
        return [.. errors];
    }
}
```

---

## 6. Additional Guidelines

### Async/Await

Always use `.ConfigureAwait(false)` in library code:

```csharp
public async Task<ImmutableArray<TaskItem>> GetAllTasksAsync()
{
    var document = await ReadDocumentAsync().ConfigureAwait(false);
    return document.Tasks;
}
```

### Nullable Reference Types

Handle nulls explicitly:

```csharp
// Use null-forgiving operator only when guaranteed non-null
[Inject] private ITaskStore TaskStore { get; set; } = default!;

// Use null-conditional and null-coalescing
private string GetTaskName(TaskItem? task) => task?.Name ?? "Unnamed";

// Guard against null
if (task is null) return;
```

### Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Private fields | `_camelCase` | `_tasks`, `_isLoading` |
| Public properties | `PascalCase` | `Tasks`, `IsLoading` |
| Methods | `PascalCase` | `LoadTasksAsync`, `GetStatusColor` |
| Local variables | `camelCase` | `task`, `filteredTasks` |
| Constants | `PascalCase` | `MaxRetries`, `DefaultTimeout` |

### Component Parameters

```csharp
// Required parameters
[Parameter, EditorRequired] public TaskItem Task { get; set; } = default!;

// Optional parameters with defaults
[Parameter] public bool ShowActions { get; set; } = true;

// Event callbacks
[Parameter] public EventCallback<TaskItem> OnTaskSelected { get; set; }

// Cascading parameters
[CascadingParameter] public MudDialogInstance? MudDialog { get; set; }
```

---

## Quick Reference Checklist

Before submitting code, verify:

- [ ] Each component has single responsibility
- [ ] Dependencies are injected as interfaces
- [ ] Records used for data models
- [ ] ImmutableArray used for collections
- [ ] No method exceeds complexity of 6
- [ ] Code-behind pattern used (no `@code` blocks)
- [ ] Static methods used where appropriate
- [ ] Async methods use `.ConfigureAwait(false)`
- [ ] Nullable reference types handled
- [ ] Naming conventions followed
