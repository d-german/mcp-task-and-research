using System.ComponentModel;
using System.Collections.Immutable;
using Mcp.TaskAndResearch.Data;

namespace Mcp.TaskAndResearch.Tools.Task;

internal sealed record RelatedFileInput
{
    [Description("File path, relative to the project root or absolute path.")]
    public required string Path { get; init; }

    [Description("File relationship type, such as TO_MODIFY or REFERENCE.")]
    public required string Type { get; init; }

    [Description("Optional description of the file.")]
    public string? Description { get; init; }

    [Description("Optional start line number (1-based).")]
    public int? LineStart { get; init; }

    [Description("Optional end line number (1-based).")]
    public int? LineEnd { get; init; }
}

internal sealed record TaskInput
{
    [Description("Task name.")]
    public required string Name { get; init; }

    [Description("Task description.")]
    public required string Description { get; init; }

    [Description("Optional task notes.")]
    public string? Notes { get; init; }

    [Description("Optional dependency list (task IDs or task names).")]
    public string[]? Dependencies { get; init; }

    [Description("Optional related files list.")]
    public RelatedFileInput[]? RelatedFiles { get; init; }

    [Description("Optional implementation guide.")]
    public string? ImplementationGuide { get; init; }

    [Description("Optional verification criteria.")]
    public string? VerificationCriteria { get; init; }

    [Description("Optional agent assignment.")]
    public string? Agent { get; init; }
}

internal sealed record TaskBatchResult
{
    public required ImmutableArray<TaskItem> CreatedTasks { get; init; }
    public required ImmutableArray<TaskItem> AllTasks { get; init; }
}

internal sealed record TaskUpdateTarget
{
    public required TaskItem Existing { get; init; }
    public required TaskInput Input { get; init; }
}
