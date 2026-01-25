using System.Collections.Immutable;

namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Repository interface for task data operations.
/// Extends ITaskReader with write operations.
/// </summary>
internal interface ITaskRepository : ITaskReader
{
    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="request">The task creation request.</param>
    /// <returns>The created task.</returns>
    Task<TaskItem> CreateAsync(TaskCreateRequest request);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="taskId">The task ID to update.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated task, or null if not found.</returns>
    Task<TaskItem?> UpdateAsync(string taskId, TaskUpdateRequest request);

    /// <summary>
    /// Deletes a task.
    /// </summary>
    /// <param name="taskId">The task ID to delete.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string taskId);

    /// <summary>
    /// Clears all tasks, backing up completed ones.
    /// </summary>
    /// <returns>Result of the clear operation.</returns>
    Task<ClearAllResult> ClearAllAsync();
}
