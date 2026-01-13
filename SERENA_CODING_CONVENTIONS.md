# Serena Coding Conventions Summary

This document summarizes coding conventions for the MCP Task Manager project, with
Result<T>/Maybe<T> fluent railway-oriented patterns, minimal try/catch usage, and guidance for
functional programming with CSharpFunctionalExtensions 3.0.0.

## Core conventions (from Serena memory)

- Use Result<T> and Maybe for error handling instead of exceptions.
- Prefer fluent chains with Ensure/Bind/Map/Tap (railway-oriented programming).
- Favor immutability and minimize mutable state.
- Use dependency injection; depend on interfaces.
- Follow naming conventions: private fields use underscore, methods use PascalCase.
- Add XML documentation for public APIs.
- Keep methods static if they do not rely on instance state.
- Keep code SOLID and focused (small interfaces, single responsibility).

## Railway-Oriented Programming with Result<T>

The project uses CSharpFunctionalExtensions to keep error handling explicit and composable.
All data layer and service operations return Result<T> or Maybe<T> instead of throwing exceptions.

### When to use Result vs Maybe

- **Result<T>**: Operations that can fail with meaningful error messages
  - Example: `TaskStore.UpdateAsync()` returns `Result<TaskItem>` - can fail if task not found
  - Example: `MemoryStore.WriteSnapshotAsync()` returns `Result<string>` - can fail on IO errors

- **Maybe<T>**: Operations that may or may not return a value (like nullable, but functional)
  - Example: `TaskStore.GetByIdAsync()` returns `Maybe<TaskItem>` - task may not exist
  - Example: `RulesStore.ReadAsync()` returns `Maybe<string>` - file may not exist

### Real-world examples from TaskStore

Example 1: Update operation with validation and persistence

```csharp
public async Task<Result<TaskItem>> UpdateAsync(string taskId, TaskUpdateRequest request)
{
    return await GetByIdAsync(taskId)
        .ToResult("Task not found")
        .Ensure(item => !string.IsNullOrWhiteSpace(request.Name ?? item.Name), "Name cannot be empty")
        .Map(item => item with 
        { 
            Name = request.Name ?? item.Name,
            Status = request.Status ?? item.Status,
            UpdatedAt = DateTimeOffset.UtcNow
        })
        .TapAsync(async updated => await WriteTasksAsync(_tasks.Replace(updated)))
        .ConfigureAwait(false);
}
```

Example 2: Complex fluent chain with multiple async operations (TaskBatchService)

```csharp
public async Task<Result<TaskBatchResult>> ApplyAsync(TaskBatchRequest request)
{
    return await _inputValidator
        .ValidateUniqueNames(request.Tasks)
        .BindAsync(async _ => await BuildDependencyGraphAsync(request.Tasks))
        .BindAsync(async graph => await CreateTasksAsync(request.Tasks, graph, request.UpdateMode))
        .TapAsync(async result => await NotifyChangesAsync(result))
        .ConfigureAwait(false);
}
```

### AsyncResultExtensions for async chains

The project includes custom extension methods for async Result operations with proper `ConfigureAwait(false)`:

```csharp
// Custom extensions in src/Mcp.TaskAndResearch/Extensions/ResultExtensions.cs
public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<TOut>> mapper)
public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> binder)
public static async Task<Result<T>> EnsureAsync<T>(this Result<T> result, Func<T, Task<bool>> predicate, string error)
public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action)
```

### Error handling with domain errors

All errors use domain-specific types from `src/Mcp.TaskAndResearch/Data/Errors.cs`:

```csharp
// Domain error types
public interface IDomainError { string Message { get; } }
public record TaskNotFoundError(string TaskId) : IDomainError;
public record TaskValidationError(string Details) : IDomainError;
public record FileOperationError(string Operation, string Details) : IDomainError;
public record DependencyResolutionError(string Details) : IDomainError;

// Usage in code
return Result.Failure<TaskItem>(new TaskNotFoundError(taskId).Message);
```

## Minimal try/catch guidance

- **Try/catch only at system boundaries**: UI components (Blazor), MCP tool interfaces
- **Use `AsyncResultExtensions.TryAsync`** to wrap dangerous operations:
  ```csharp
  return await AsyncResultExtensions.TryAsync(async () =>
  {
      var json = await File.ReadAllTextAsync(path);
      return JsonSerializer.Deserialize<TaskItem[]>(json);
  }).ConfigureAwait(false);
  ```
- **UI boundary pattern** (from TaskDetailDialog.razor.cs):
  ```csharp
  var updateResult = await TaskStore.UpdateAsync(Task.Id, request);
  if (updateResult.IsSuccess)
  {
      Snackbar.Add("Task saved successfully", Severity.Success);
  }
  else
  {
      Snackbar.Add($"Failed to save task: {updateResult.Error}", Severity.Error);
  }
  ```
- **MCP tool pattern** (from TaskTools.cs) - convert Result to string for MCP interface:
  ```csharp
  public async Task<string> GetTaskDetail(string taskId)
  {
      return await _taskStore.GetByIdAsync(taskId)
          .ToResult($"Task not found: {taskId}")
          .BindAsync(async task => await _promptBuilder.BuildAsync(task))
          .Match(
              onSuccess: prompt => prompt,
              onFailure: error => $"Error: {error}"
          );
  }
  ```

## Conversion patterns used in this refactoring

### Before (imperative with try/catch):
```csharp
public async Task<TaskItem?> UpdateAsync(string taskId, TaskUpdateRequest request)
{
    try
    {
        var task = await GetByIdAsync(taskId);
        if (task == null)
            return null;
        
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name required");
        
        var updated = task with { Name = request.Name, UpdatedAt = DateTimeOffset.UtcNow };
        await WriteTasksAsync(_tasks.Replace(updated));
        return updated;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Update failed");
        return null;
    }
}
```

### After (functional with Result):
```csharp
public async Task<Result<TaskItem>> UpdateAsync(string taskId, TaskUpdateRequest request)
{
    return await GetByIdAsync(taskId)
        .ToResult("Task not found")
        .Ensure(item => !string.IsNullOrWhiteSpace(request.Name ?? item.Name), "Name cannot be empty")
        .Map(item => item with 
        { 
            Name = request.Name ?? item.Name,
            UpdatedAt = DateTimeOffset.UtcNow
        })
        .TapAsync(async updated => await WriteTasksAsync(_tasks.Replace(updated)))
        .ConfigureAwait(false);
}
```

### Conversion checklist
1. **Replace early returns** → Use `Ensure()` for validation
2. **Replace null checks** → Use `Maybe<T>.ToResult("error message")`
3. **Replace nested if/else** → Use fluent `Bind/Map/Tap` chains
4. **Replace try/catch** → Use `AsyncResultExtensions.TryAsync()`
5. **Side effects** → Move to `Tap()` or `TapAsync()`
6. **Business rules** → Move to `Ensure()` or `EnsureAsync()`
7. **Transformations** → Use `Map()` or `MapAsync()`
8. **Async operations** → Use `BindAsync()`, `MapAsync()`, `TapAsync()`

## Testing Result<T> patterns

From `tests/Mcp.TaskAndResearch.Tests/Data/ResultPatternTests.cs`:

```csharp
[Fact]
public async Task UpdateAsync_ExistingTask_ReturnsResultSuccess()
{
    var result = await stores.TaskStore.UpdateAsync(taskId, new TaskUpdateRequest
    {
        Name = "Updated"
    });

    Assert.True(result.IsSuccess, "UpdateAsync should succeed for existing task");
    Assert.Equal("Updated", result.Value.Name);
}

[Fact]
public async Task UpdateAsync_NonExistentTask_ReturnsResultFailure()
{
    var result = await stores.TaskStore.UpdateAsync("non-existent-id", new TaskUpdateRequest
    {
        Name = "Updated"
    });

    Assert.True(result.IsFailure, "UpdateAsync should fail for non-existent task");
    Assert.Contains("not found", result.Error.ToLowerInvariant());
}
```

## Architecture layers

**Data Layer** (`src/Mcp.TaskAndResearch/Data/`)
- All operations return `Result<T>` or `Maybe<T>`
- `TaskStore`, `MemoryStore`, `RulesStore`, `TaskSearchService`

**Services Layer** (`src/Mcp.TaskAndResearch/Tools/Task/TaskServices.cs`)
- Uses fluent chains for complex workflows
- `TaskDependencyResolver`, `TaskInputValidator`, `TaskBatchService`, `TaskWorkflowService`

**Tools Layer** (`src/Mcp.TaskAndResearch/Tools/`)
- MCP tool interfaces - convert `Result<T>` to string with `.Match()`
- Use `try/catch` only at this boundary for unexpected exceptions

**UI Layer** (`src/Mcp.TaskAndResearch/UI/Components/`)
- Blazor components check `.IsSuccess`/`.IsFailure`
- Display user-friendly messages from `.Error`

## Best practices discovered

1. **Always use ConfigureAwait(false)** in library code
2. **LoadTemplateOrThrow** pattern: Built-in templates can throw, custom templates should return Result
3. **ToResult for Maybe → Result conversion**: `GetByIdAsync(id).ToResult("Not found")`
4. **Domain errors over strings**: Use typed errors from `Errors.cs` for better error handling
5. **Immutable records for requests**: `TaskCreateRequest`, `TaskUpdateRequest` use records
6. **Fluent chains over intermediate variables**: Compose operations directly
7. **Test both success and failure paths**: Every Result operation needs both test cases
