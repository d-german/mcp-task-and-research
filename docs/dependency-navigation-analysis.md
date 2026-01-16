# Dependency Navigation Feasibility Analysis

**Analysis Date:** January 15, 2026  
**Task ID:** 94425ebe-e60b-44f4-955d-f0a39d3451ee

## Executive Summary

✅ **Clickable dependency navigation is FEASIBLE and RECOMMENDED**

The codebase supports dependency resolution via `ITaskReader.GetByIdAsync(string taskId)`, and the existing UI infrastructure (MudDialog) provides an excellent navigation mechanism.

## Data Model Analysis

### TaskDependency Structure
```csharp
public sealed record TaskDependency
{
    public required string TaskId { get; init; }
}
```

**Key Findings:**
- Dependencies store **only the TaskId** (string)
- No validation that TaskIds exist at dependency creation time
- TaskIds are GUIDs, making manual entry unlikely but possible

### Task Retrieval Capabilities

#### ITaskReader.GetByIdAsync()
- **Return Type:** `Task<TaskItem?>`
- **Null Return:** Indicates task not found
- **Scope:** Returns tasks from **active store only** (TaskStore)
- **Does NOT search:** MemoryStore (archived/cleared tasks)

#### MemoryStore (Archived Tasks)
- Contains tasks cleared via `ClearAllAsync()`
- Only completed tasks are archived
- MemoryStore is **not queried** by `GetByIdAsync()`

## Edge Cases & Handling Strategy

| Edge Case | Likelihood | Detection | Recommended Handling |
|-----------|-----------|-----------|---------------------|
| **Non-existent Task ID** | Medium | `GetByIdAsync()` returns null | Show Snackbar: "Task not found (may have been deleted)" |
| **Archived/Cleared Task** | Low-Medium | `GetByIdAsync()` returns null | Same as above - transparent to user |
| **Circular Dependencies** | Low | Not currently validated | No immediate impact on navigation; future enhancement |
| **Self-referencing Task** | Very Low | No validation | Would open same task's dialog again (harmless) |
| **Invalid TaskId Format** | Very Low | Guid parse would fail | Treat as non-existent |

### Additional Scenarios
- **Pending Tasks with Dependencies:** ✅ Navigation works normally
- **Completed Tasks with Dependencies:** ✅ Navigation works normally  
- **In-Progress Tasks with Dependencies:** ✅ Navigation works normally

## Recommended Navigation Mechanism

### ✅ Option 1: Dialog (PREFERRED)

**Approach:** Open `TaskDetailDialog` in modal  
**Advantages:**
- Existing infrastructure (`TaskDetailDialog.razor` already built)
- Non-destructive (doesn't lose current context)
- Follows "drill-down" pattern
- Supports navigation chains (dependency of dependency)
- Consistent with current tasks page behavior

**Implementation:**
```csharp
private async Task NavigateToDependency(string taskId)
{
    var task = await TaskReader.GetByIdAsync(taskId);
    
    if (task is null)
    {
        Snackbar.Add("Task not found (may have been deleted)", Severity.Warning);
        return;
    }

    var parameters = new DialogParameters<TaskDetailDialog>
    {
        { x => x.Task, task }
    };

    await DialogService.ShowAsync<TaskDetailDialog>(
        $"Task: {task.Name}",
        parameters,
        new DialogOptions { MaxWidth = MaxWidth.Medium });
}
```

### ❌ Option 2: Scroll-to

**Why NOT Recommended:**
- Only works if dependency is visible on current page
- History page shows timeline events, not all tasks
- Dependencies may not be present in filtered view
- Poor UX for tasks not currently displayed

### ❌ Option 3: Page Navigation

**Why NOT Recommended:**
- No dedicated task detail page exists
- Would need to create new route
- Loses current context (filter state, scroll position)
- Browser back button confusion

## Data Integrity Considerations

### Current State
- **No referential integrity:** TaskIds in dependencies are not validated
- **Orphaned dependencies possible:** If a task is deleted, dependencies pointing to it remain

### Future Enhancements (Out of Scope for Current Task)
1. **Dependency validation** on task creation/update
2. **Cascade options** on task deletion (remove dependency references vs. block deletion)
3. **Circular dependency detection** (graph cycle check)
4. **Dependency name resolution** (cache task names with TaskId for better display)

## Implementation Requirements

### Minimal Changes Needed

1. **TaskDetailView.razor** - Add EventCallback parameter:
```csharp
[Parameter]
public EventCallback<string> OnDependencyClick { get; set; }
```

2. **TaskDetailView.razor** - Make chips clickable:
```html
<MudChip T="string" 
         Size="Size.Small" 
         Color="Color.Info"
         OnClick="@(() => OnDependencyClick.InvokeAsync(dep.TaskId))">
    @dep.TaskId
</MudChip>
```

3. **Parent Components** (HistoryView, TaskDetailDialog) - Handle callback:
```csharp
private async Task OnDependencyClicked(string taskId)
{
    var task = await TaskReader.GetByIdAsync(taskId);
    
    if (task is null)
    {
        Snackbar.Add("Task not found (may have been deleted)", Severity.Warning);
        return;
    }

    var parameters = new DialogParameters<TaskDetailDialog>
    {
        { x => x.Task, task }
    };

    await DialogService.ShowAsync<TaskDetailDialog>(
        $"Task: {task.Name}",
        parameters);
}
```

### Required Services
- ✅ `ITaskReader` - Already injected in parent components
- ✅ `IDialogService` - Already available via MudBlazor
- ✅ `ISnackbar` - Already injected in parent components

## Conclusion

**RECOMMENDATION: Implement Option 1 (Dialog-based Navigation)**

- ✅ Technically feasible with existing infrastructure
- ✅ Graceful handling of edge cases (null tasks)
- ✅ Consistent with application patterns
- ✅ Minimal code changes required
- ✅ Supports iterative navigation (dependency chains)
- ✅ Non-breaking change (additive only)

**Estimated Complexity:** LOW-MEDIUM  
**Estimated Implementation Time:** 1-2 hours

## References

- [TaskModels.cs](../src/Mcp.TaskAndResearch/Data/TaskModels.cs) - TaskDependency definition
- [ITaskReader.cs](../src/Mcp.TaskAndResearch/Data/ITaskReader.cs) - GetByIdAsync interface
- [TaskStore.cs](../src/Mcp.TaskAndResearch/Data/TaskStore.cs) - Implementation
- [TaskDetailDialog.razor](../src/Mcp.TaskAndResearch/UI/Components/Dialogs/TaskDetailDialog.razor) - Existing dialog
- [TaskDetailView.razor](../src/Mcp.TaskAndResearch/UI/Components/Shared/TaskDetailView.razor) - Shared component
