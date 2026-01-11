namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Event arguments for task change notifications.
/// </summary>
public sealed class TaskChangeEventArgs : EventArgs
{
    public required TaskChangeType ChangeType { get; init; }
    public TaskItem? Task { get; init; }
    public string? PreviousId { get; init; }

    public static TaskChangeEventArgs Created(TaskItem task) => new()
    {
        ChangeType = TaskChangeType.Created,
        Task = task
    };

    public static TaskChangeEventArgs Updated(TaskItem task) => new()
    {
        ChangeType = TaskChangeType.Updated,
        Task = task
    };

    public static TaskChangeEventArgs Deleted(TaskItem task) => new()
    {
        ChangeType = TaskChangeType.Deleted,
        Task = task
    };

    public static TaskChangeEventArgs Cleared() => new()
    {
        ChangeType = TaskChangeType.Cleared,
        Task = null!
    };
}

public enum TaskChangeType
{
    Created,
    Updated,
    Deleted,
    Cleared
}
