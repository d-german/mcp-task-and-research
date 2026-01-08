using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Mcp.TaskAndResearch.Data;

[JsonConverter(typeof(TaskStatusJsonConverter))]
internal enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Blocked
}

internal enum RelatedFileType
{
    TO_MODIFY,
    REFERENCE,
    CREATE,
    DEPENDENCY,
    OTHER
}

internal sealed record TaskDependency
{
    public required string TaskId { get; init; }
}

internal sealed record RelatedFile
{
    public required string Path { get; init; }
    public required RelatedFileType Type { get; init; }
    public string? Description { get; init; }
    public int? LineStart { get; init; }
    public int? LineEnd { get; init; }
}

internal sealed record TaskItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? Notes { get; init; }
    public required TaskStatus Status { get; init; }
    public ImmutableArray<TaskDependency> Dependencies { get; init; } = ImmutableArray<TaskDependency>.Empty;
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? Summary { get; init; }
    public ImmutableArray<RelatedFile> RelatedFiles { get; init; } = ImmutableArray<RelatedFile>.Empty;
    public string? AnalysisResult { get; init; }
    public string? Agent { get; init; }
    public string? ImplementationGuide { get; init; }
    public string? VerificationCriteria { get; init; }
}

internal sealed record TaskDocument
{
    public required ImmutableArray<TaskItem> Tasks { get; init; }

    public static TaskDocument Empty { get; } = new()
    {
        Tasks = ImmutableArray<TaskItem>.Empty
    };
}
