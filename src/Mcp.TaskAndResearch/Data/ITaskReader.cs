using System.Collections.Immutable;
using CSharpFunctionalExtensions;

namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Public interface for reading tasks from the store with change notifications.
/// Used by the UI layer to observe and display task data.
/// </summary>
public interface ITaskReader
{
    /// <summary>
    /// Event raised when tasks are created, updated, deleted, or cleared.
    /// </summary>
    event Action<TaskChangeEventArgs>? OnTaskChanged;

    /// <summary>
    /// Gets all tasks.
    /// </summary>
    Task<Result<ImmutableArray<TaskItem>>> GetAllAsync();

    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    Task<Maybe<TaskItem>> GetByIdAsync(string taskId);
}
