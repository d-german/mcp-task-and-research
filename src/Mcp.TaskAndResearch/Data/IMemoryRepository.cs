using System.Collections.Immutable;

namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Repository interface for task history/snapshot operations.
/// </summary>
public interface IMemoryRepository
{
    /// <summary>
    /// Writes a snapshot of tasks to storage.
    /// </summary>
    /// <param name="tasks">The tasks to snapshot.</param>
    /// <returns>The snapshot identifier (file name or ID).</returns>
    Task<string> WriteSnapshotAsync(ImmutableArray<TaskItem> tasks);

    /// <summary>
    /// Reads all tasks from all snapshots.
    /// </summary>
    /// <returns>All tasks from history snapshots.</returns>
    Task<ImmutableArray<TaskItem>> ReadAllSnapshotsAsync();
}
