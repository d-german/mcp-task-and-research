using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Data;
using TaskStatus = Mcp.TaskAndResearch.Data.TaskStatus;

namespace Mcp.TaskAndResearch.Tools.Task;

internal enum TaskUpdateMode
{
    Append,
    Overwrite,
    Selective,
    ClearAllTasks
}

internal static class TaskUpdateModeParser
{
    private static readonly IReadOnlyDictionary<string, TaskUpdateMode> ModeMap =
        new Dictionary<string, TaskUpdateMode>(StringComparer.OrdinalIgnoreCase)
        {
            ["append"] = TaskUpdateMode.Append,
            ["overwrite"] = TaskUpdateMode.Overwrite,
            ["selective"] = TaskUpdateMode.Selective,
            ["clearalltasks"] = TaskUpdateMode.ClearAllTasks,
            ["clear_all_tasks"] = TaskUpdateMode.ClearAllTasks
        };

    public static TaskUpdateMode Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TaskUpdateMode.ClearAllTasks;
        }

        return ModeMap.TryGetValue(value.Trim(), out var mode)
            ? mode
            : TaskUpdateMode.ClearAllTasks;
    }
}

internal sealed record TaskUpdatePlan
{
    public required ImmutableArray<TaskInput> TasksToCreate { get; init; }
    public required ImmutableArray<TaskUpdateTarget> TasksToUpdate { get; init; }
    public required ImmutableArray<TaskItem> TasksToRemove { get; init; }
}

internal sealed class TaskUpdatePlanner
{
    public TaskUpdatePlan BuildPlan(
        TaskUpdateMode mode,
        ImmutableArray<TaskInput> inputs,
        ImmutableArray<TaskItem> existing)
    {
        return mode switch
        {
            TaskUpdateMode.Append => BuildAppendPlan(inputs),
            TaskUpdateMode.Overwrite => BuildOverwritePlan(inputs, existing),
            TaskUpdateMode.Selective => BuildSelectivePlan(inputs, existing),
            _ => BuildClearAllPlan(inputs, existing)
        };
    }

    private static TaskUpdatePlan BuildAppendPlan(ImmutableArray<TaskInput> inputs)
    {
        return new TaskUpdatePlan
        {
            TasksToCreate = inputs,
            TasksToUpdate = ImmutableArray<TaskUpdateTarget>.Empty,
            TasksToRemove = ImmutableArray<TaskItem>.Empty
        };
    }

    private static TaskUpdatePlan BuildOverwritePlan(
        ImmutableArray<TaskInput> inputs,
        ImmutableArray<TaskItem> existing)
    {
        var toRemove = existing.Where(task => task.Status != TaskStatus.Completed).ToImmutableArray();
        return new TaskUpdatePlan
        {
            TasksToCreate = inputs,
            TasksToUpdate = ImmutableArray<TaskUpdateTarget>.Empty,
            TasksToRemove = toRemove
        };
    }

    private static TaskUpdatePlan BuildClearAllPlan(
        ImmutableArray<TaskInput> inputs,
        ImmutableArray<TaskItem> existing)
    {
        return new TaskUpdatePlan
        {
            TasksToCreate = inputs,
            TasksToUpdate = ImmutableArray<TaskUpdateTarget>.Empty,
            TasksToRemove = existing
        };
    }

    private static TaskUpdatePlan BuildSelectivePlan(
        ImmutableArray<TaskInput> inputs,
        ImmutableArray<TaskItem> existing)
    {
        var lookup = existing.ToDictionary(task => task.Name, StringComparer.Ordinal);
        var toCreate = ImmutableArray.CreateBuilder<TaskInput>();
        var toUpdate = ImmutableArray.CreateBuilder<TaskUpdateTarget>();
        var toRemove = ImmutableArray.CreateBuilder<TaskItem>();

        foreach (var input in inputs)
        {
            if (lookup.TryGetValue(input.Name, out var task))
            {
                if (task.Status == TaskStatus.Completed)
                {
                    toRemove.Add(task);
                    continue;
                }

                toUpdate.Add(new TaskUpdateTarget { Existing = task, Input = input });
                continue;
            }

            toCreate.Add(input);
        }

        return new TaskUpdatePlan
        {
            TasksToCreate = toCreate.ToImmutable(),
            TasksToUpdate = toUpdate.ToImmutable(),
            TasksToRemove = toRemove.ToImmutable()
        };
    }
}

internal sealed class TaskBatchService
{
    private readonly TaskStore _taskStore;
    private readonly TaskUpdatePlanner _planner;

    public TaskBatchService(TaskStore taskStore, TaskUpdatePlanner planner)
    {
        _taskStore = taskStore;
        _planner = planner;
    }

    public async Task<Result<TaskBatchResult>> ApplyAsync(
        TaskUpdateMode mode,
        ImmutableArray<TaskInput> inputs,
        string? globalAnalysisResult)
    {
        var existingResult = await _taskStore.GetAllAsync().ConfigureAwait(false);
        if (existingResult.IsFailure)
        {
            return Result.Failure<TaskBatchResult>(existingResult.Error);
        }

        var existing = existingResult.Value;
        var plan = _planner.BuildPlan(mode, inputs, existing);

        return await ApplyRemovalsAsync(mode, plan.TasksToRemove)
            .Bind(() => ApplyUpdatesAsync(plan.TasksToUpdate, globalAnalysisResult))
            .Bind(updatedTasks => ApplyCreatesAsync(plan.TasksToCreate, globalAnalysisResult)
                .Map(createdTasks => (Updated: updatedTasks, Created: createdTasks)))
            .Bind(async results =>
            {
                var applyDepsResult = await ApplyDependenciesAsync(inputs).ConfigureAwait(false);
                if (applyDepsResult.IsFailure)
                {
                    return Result.Failure<TaskBatchResult>(applyDepsResult.Error);
                }

                var allTasksResult = await _taskStore.GetAllAsync().ConfigureAwait(false);
                if (allTasksResult.IsFailure)
                {
                    return Result.Failure<TaskBatchResult>(allTasksResult.Error);
                }

                var allTasks = allTasksResult.Value;
                var createdOrUpdated = MergeTasks(results.Created, results.Updated);
                return Result.Success(new TaskBatchResult
                {
                    CreatedTasks = createdOrUpdated,
                    AllTasks = allTasks
                });
            })
            .ConfigureAwait(false);
    }

    private async Task<Result> ApplyRemovalsAsync(TaskUpdateMode mode, ImmutableArray<TaskItem> toRemove)
    {
        if (mode == TaskUpdateMode.ClearAllTasks)
        {
            var clearResult = await _taskStore.ClearAllAsync().ConfigureAwait(false);
            return clearResult.IsSuccess ? Result.Success() : Result.Failure(clearResult.Error);
        }

        foreach (var task in toRemove)
        {
            var deleteResult = await _taskStore.DeleteAsync(task.Id).ConfigureAwait(false);
            if (deleteResult.IsFailure)
            {
                return Result.Failure(deleteResult.Error);
            }
        }

        return Result.Success();
    }

    private async Task<Result<ImmutableArray<TaskItem>>> ApplyUpdatesAsync(
        ImmutableArray<TaskUpdateTarget> targets,
        string? globalAnalysisResult)
    {
        if (targets.IsDefaultOrEmpty)
        {
            return Result.Success(ImmutableArray<TaskItem>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<TaskItem>();
        foreach (var target in targets)
        {
            var request = BuildUpdateRequest(target.Input, globalAnalysisResult);
            var updateResult = await _taskStore.UpdateAsync(target.Existing.Id, request).ConfigureAwait(false);
            if (updateResult.IsFailure)
            {
                return Result.Failure<ImmutableArray<TaskItem>>(updateResult.Error);
            }
            builder.Add(updateResult.Value);
        }

        return Result.Success(builder.ToImmutable());
    }

    private async Task<Result<ImmutableArray<TaskItem>>> ApplyCreatesAsync(
        ImmutableArray<TaskInput> inputs,
        string? globalAnalysisResult)
    {
        if (inputs.IsDefaultOrEmpty)
        {
            return Result.Success(ImmutableArray<TaskItem>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<TaskItem>();
        foreach (var input in inputs)
        {
            var request = BuildCreateRequest(input, globalAnalysisResult);
            var createResult = await _taskStore.CreateAsync(request).ConfigureAwait(false);
            if (createResult.IsFailure)
            {
                return Result.Failure<ImmutableArray<TaskItem>>(createResult.Error);
            }
            builder.Add(createResult.Value);
        }

        return Result.Success(builder.ToImmutable());
    }

    private async Task<Result> ApplyDependenciesAsync(ImmutableArray<TaskInput> inputs)
    {
        if (inputs.IsDefaultOrEmpty)
        {
            return Result.Success();
        }

        var allTasksResult = await _taskStore.GetAllAsync().ConfigureAwait(false);
        if (allTasksResult.IsFailure)
        {
            return Result.Failure(allTasksResult.Error);
        }

        var allTasks = allTasksResult.Value;
        var nameMap = TaskNameMap.Build(allTasks);
        var idSet = allTasks.Select(task => task.Id).ToImmutableHashSet();

        foreach (var input in inputs)
        {
            if (!nameMap.TryGetValue(input.Name, out var taskId))
            {
                continue;
            }

            var dependenciesResult = TaskDependencyResolver.Resolve(input.Dependencies, nameMap, idSet);
            if (dependenciesResult.IsFailure)
            {
                continue; // Skip unresolvable dependencies for now, will be handled by validation
            }

            var request = new TaskUpdateRequest
            {
                Dependencies = dependenciesResult.Value
            };

            var updateResult = await _taskStore.UpdateAsync(taskId, request).ConfigureAwait(false);
            if (updateResult.IsFailure)
            {
                return Result.Failure($"Failed to apply dependencies for task '{input.Name}': {updateResult.Error}");
            }
        }

        return Result.Success();
    }

    private static TaskCreateRequest BuildCreateRequest(TaskInput input, string? globalAnalysisResult)
    {
        return new TaskCreateRequest
        {
            Name = input.Name,
            Description = input.Description,
            Notes = input.Notes,
            Dependencies = ImmutableArray<string>.Empty,
            RelatedFiles = RelatedFileConverter.ToRelatedFiles(input.RelatedFiles),
            AnalysisResult = globalAnalysisResult,
            Agent = input.Agent,
            ImplementationGuide = input.ImplementationGuide,
            VerificationCriteria = input.VerificationCriteria
        };
    }

    private static TaskUpdateRequest BuildUpdateRequest(TaskInput input, string? globalAnalysisResult)
    {
        return new TaskUpdateRequest
        {
            Name = input.Name,
            Description = input.Description,
            Notes = input.Notes,
            RelatedFiles = RelatedFileConverter.ToRelatedFiles(input.RelatedFiles),
            AnalysisResult = globalAnalysisResult,
            Agent = input.Agent,
            ImplementationGuide = input.ImplementationGuide,
            VerificationCriteria = input.VerificationCriteria
        };
    }

    private static ImmutableArray<TaskItem> MergeTasks(
        ImmutableArray<TaskItem> created,
        ImmutableArray<TaskItem> updated)
    {
        if (updated.IsDefaultOrEmpty)
        {
            return created;
        }

        return created.AddRange(updated);
    }
}

internal static class RelatedFileConverter
{
    public static ImmutableArray<RelatedFile> ToRelatedFiles(RelatedFileInput[]? inputs)
    {
        if (inputs is null || inputs.Length == 0)
        {
            return ImmutableArray<RelatedFile>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<RelatedFile>();
        foreach (var input in inputs)
        {
            builder.Add(new RelatedFile
            {
                Path = input.Path,
                Type = RelatedFileTypeParser.Parse(input.Type),
                Description = input.Description,
                LineStart = input.LineStart,
                LineEnd = input.LineEnd
            });
        }

        return builder.ToImmutable();
    }
}

internal static class RelatedFileTypeParser
{
    public static RelatedFileType Parse(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            Enum.TryParse<RelatedFileType>(value, true, out var parsed))
        {
            return parsed;
        }

        return RelatedFileType.OTHER;
    }
}

internal static class RelatedFileValidator
{
    public static string? ValidateLineNumbers(RelatedFileInput[]? inputs)
    {
        if (inputs is null || inputs.Length == 0)
        {
            return null;
        }

        foreach (var input in inputs)
        {
            if (HasInvalidLineRange(input))
            {
                return "Invalid line number settings: start and end line must both be provided, and start line must be <= end line.";
            }
        }

        return null;
    }

    private static bool HasInvalidLineRange(RelatedFileInput input)
    {
        var hasStart = input.LineStart.HasValue;
        var hasEnd = input.LineEnd.HasValue;

        if (hasStart != hasEnd)
        {
            return true;
        }

        if (hasStart && hasEnd)
        {
            return input.LineStart > input.LineEnd;
        }

        return false;
    }
}

internal static class TaskNameMap
{
    public static Dictionary<string, string> Build(ImmutableArray<TaskItem> tasks)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var task in tasks)
        {
            map[task.Name] = task.Id;
        }

        return map;
    }
}

internal static class TaskDependencyResolver
{
    private static readonly Regex GuidRegex = new(
        "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Result<ImmutableArray<string>> Resolve(
        string[]? dependencies,
        IReadOnlyDictionary<string, string> nameMap,
        ISet<string> idSet)
    {
        if (dependencies is null || dependencies.Length == 0)
        {
            return Result.Success(ImmutableArray<string>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<string>();
        var errors = new List<string>();

        foreach (var dependency in dependencies)
        {
            var resolved = ResolveDependency(dependency, nameMap, idSet);
            if (resolved.HasValue)
            {
                builder.Add(resolved.Value);
            }
            else if (!string.IsNullOrWhiteSpace(dependency))
            {
                errors.Add($"Could not resolve dependency: '{dependency}'");
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure<ImmutableArray<string>>(
                new DependencyResolutionError(string.Join("; ", errors)).Message);
        }

        return Result.Success(builder.ToImmutable());
    }

    private static Maybe<string> ResolveDependency(
        string? dependency,
        IReadOnlyDictionary<string, string> nameMap,
        ISet<string> idSet)
    {
        if (string.IsNullOrWhiteSpace(dependency))
        {
            return Maybe<string>.None;
        }

        var trimmed = dependency.Trim();
        if (GuidRegex.IsMatch(trimmed))
        {
            return idSet.Contains(trimmed) ? Maybe.From(trimmed) : Maybe<string>.None;
        }

        return nameMap.TryGetValue(trimmed, out var taskId) 
            ? Maybe.From(taskId) 
            : Maybe<string>.None;
    }
}

internal static class TaskInputValidator
{
    public static Result ValidateUniqueNames(TaskInput[]? tasks)
    {
        if (tasks is null || tasks.Length == 0)
        {
            return Result.Failure(
                new TaskValidationError("No tasks were provided.").Message);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var task in tasks)
        {
            if (!seen.Add(task.Name))
            {
                return Result.Failure(
                    new TaskValidationError("Duplicate task names detected. Ensure each task name is unique.").Message);
            }
        }

        return Result.Success();
    }
}

internal sealed record TaskExecutionCheck(bool CanExecute, ImmutableArray<string> BlockedBy)
{
    public static TaskExecutionCheck Allowed { get; } =
        new(true, ImmutableArray<string>.Empty);
}

internal sealed class TaskWorkflowService
{
    private readonly TaskStore _taskStore;
    private readonly TaskComplexityAssessor _complexityAssessor;
    private readonly RelatedFilesSummaryBuilder _summaryBuilder;

    public TaskWorkflowService(
        TaskStore taskStore,
        TaskComplexityAssessor complexityAssessor,
        RelatedFilesSummaryBuilder summaryBuilder)
    {
        _taskStore = taskStore;
        _complexityAssessor = complexityAssessor;
        _summaryBuilder = summaryBuilder;
    }

    public TaskComplexityAssessment AssessComplexity(TaskItem task)
    {
        return _complexityAssessor.Assess(task);
    }

    public async Task<Result<TaskExecutionCheck>> CanExecuteAsync(TaskItem task)
    {
        if (task.Status == TaskStatus.Completed)
        {
            return Result.Success(new TaskExecutionCheck(false, ImmutableArray<string>.Empty));
        }

        if (task.Dependencies.IsDefaultOrEmpty)
        {
            return Result.Success(TaskExecutionCheck.Allowed);
        }

        var allTasksResult = await _taskStore.GetAllAsync().ConfigureAwait(false);
        if (allTasksResult.IsFailure)
        {
            return Result.Failure<TaskExecutionCheck>(allTasksResult.Error);
        }

        var allTasks = allTasksResult.Value;
        var blocked = FindBlockedDependencies(task.Dependencies, allTasks);

        return Result.Success(new TaskExecutionCheck(blocked.IsDefaultOrEmpty, blocked));
    }

    public async Task<Result<TaskItem>> UpdateStatusAsync(string taskId, TaskStatus status)
    {
        var request = new TaskUpdateRequest { Status = status };
        return await _taskStore.UpdateAsync(taskId, request).ConfigureAwait(false);
    }

    public async Task<Result<TaskItem>> UpdateSummaryAsync(string taskId, string summary)
    {
        var request = new TaskUpdateRequest { Summary = summary };
        return await _taskStore.UpdateAsync(taskId, request).ConfigureAwait(false);
    }

    public async Task<Result<ImmutableArray<TaskItem>>> LoadDependencyTasksAsync(TaskItem task)
    {
        if (task.Dependencies.IsDefaultOrEmpty)
        {
            return Result.Success(ImmutableArray<TaskItem>.Empty);
        }

        var allTasksResult = await _taskStore.GetAllAsync().ConfigureAwait(false);
        if (allTasksResult.IsFailure)
        {
            return Result.Failure<ImmutableArray<TaskItem>>(allTasksResult.Error);
        }

        var allTasks = allTasksResult.Value;
        var builder = ImmutableArray.CreateBuilder<TaskItem>();
        foreach (var dependency in task.Dependencies)
        {
            var match = allTasks.FirstOrDefault(item => item.Id == dependency.TaskId);
            if (match is not null)
            {
                builder.Add(match);
            }
        }

        return Result.Success(builder.ToImmutable());
    }

    public string BuildRelatedFilesSummary(TaskItem task)
    {
        return _summaryBuilder.BuildSummary(task.RelatedFiles);
    }

    private static ImmutableArray<string> FindBlockedDependencies(
        ImmutableArray<TaskDependency> dependencies,
        ImmutableArray<TaskItem> allTasks)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var dependency in dependencies)
        {
            var match = allTasks.FirstOrDefault(task => task.Id == dependency.TaskId);
            if (match is null || match.Status != TaskStatus.Completed)
            {
                builder.Add(dependency.TaskId);
            }
        }

        return builder.ToImmutable();
    }
}

internal enum TaskComplexityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    VeryHigh = 3
}

internal sealed record TaskComplexityMetrics
{
    public required int DescriptionLength { get; init; }
    public required int DependenciesCount { get; init; }
    public required int NotesLength { get; init; }
    public required bool HasNotes { get; init; }
}

internal sealed record TaskComplexityAssessment
{
    public required TaskComplexityLevel Level { get; init; }
    public required TaskComplexityMetrics Metrics { get; init; }
    public required ImmutableArray<string> Recommendations { get; init; }
}

internal static class TaskComplexityThresholds
{
    public const int DescriptionMedium = 500;
    public const int DescriptionHigh = 1000;
    public const int DescriptionVeryHigh = 2000;

    public const int DependenciesMedium = 2;
    public const int DependenciesHigh = 5;
    public const int DependenciesVeryHigh = 10;

    public const int NotesMedium = 200;
    public const int NotesHigh = 500;
    public const int NotesVeryHigh = 1000;
}

internal sealed class TaskComplexityAssessor
{
    public TaskComplexityAssessment Assess(TaskItem task)
    {
        var metrics = BuildMetrics(task);
        var level = CalculateLevel(metrics);
        var recommendations = BuildRecommendations(level, metrics);

        return new TaskComplexityAssessment
        {
            Level = level,
            Metrics = metrics,
            Recommendations = recommendations
        };
    }

    private static TaskComplexityMetrics BuildMetrics(TaskItem task)
    {
        var notesLength = task.Notes?.Length ?? 0;
        return new TaskComplexityMetrics
        {
            DescriptionLength = task.Description.Length,
            DependenciesCount = task.Dependencies.Length,
            NotesLength = notesLength,
            HasNotes = notesLength > 0
        };
    }

    private static TaskComplexityLevel CalculateLevel(TaskComplexityMetrics metrics)
    {
        var descriptionLevel = GetLevelFromDescription(metrics.DescriptionLength);
        var dependencyLevel = GetLevelFromDependencies(metrics.DependenciesCount);
        var notesLevel = GetLevelFromNotes(metrics.NotesLength);

        return Max(descriptionLevel, dependencyLevel, notesLevel);
    }

    private static TaskComplexityLevel GetLevelFromDescription(int length)
    {
        if (length >= TaskComplexityThresholds.DescriptionVeryHigh)
        {
            return TaskComplexityLevel.VeryHigh;
        }

        if (length >= TaskComplexityThresholds.DescriptionHigh)
        {
            return TaskComplexityLevel.High;
        }

        if (length >= TaskComplexityThresholds.DescriptionMedium)
        {
            return TaskComplexityLevel.Medium;
        }

        return TaskComplexityLevel.Low;
    }

    private static TaskComplexityLevel GetLevelFromDependencies(int count)
    {
        if (count >= TaskComplexityThresholds.DependenciesVeryHigh)
        {
            return TaskComplexityLevel.VeryHigh;
        }

        if (count >= TaskComplexityThresholds.DependenciesHigh)
        {
            return TaskComplexityLevel.High;
        }

        if (count >= TaskComplexityThresholds.DependenciesMedium)
        {
            return TaskComplexityLevel.Medium;
        }

        return TaskComplexityLevel.Low;
    }

    private static TaskComplexityLevel GetLevelFromNotes(int length)
    {
        if (length >= TaskComplexityThresholds.NotesVeryHigh)
        {
            return TaskComplexityLevel.VeryHigh;
        }

        if (length >= TaskComplexityThresholds.NotesHigh)
        {
            return TaskComplexityLevel.High;
        }

        if (length >= TaskComplexityThresholds.NotesMedium)
        {
            return TaskComplexityLevel.Medium;
        }

        return TaskComplexityLevel.Low;
    }

    private static TaskComplexityLevel Max(
        TaskComplexityLevel first,
        TaskComplexityLevel second,
        TaskComplexityLevel third)
    {
        return (TaskComplexityLevel)Math.Max((int)first, Math.Max((int)second, (int)third));
    }

    private static ImmutableArray<string> BuildRecommendations(
        TaskComplexityLevel level,
        TaskComplexityMetrics metrics)
    {
        return level switch
        {
            TaskComplexityLevel.Low => BuildLowRecommendations(),
            TaskComplexityLevel.Medium => BuildMediumRecommendations(metrics),
            TaskComplexityLevel.High => BuildHighRecommendations(metrics),
            _ => BuildVeryHighRecommendations(metrics)
        };
    }

    private static ImmutableArray<string> BuildLowRecommendations()
    {
        return ImmutableArray.Create(
            "This task is low complexity and can be executed directly.",
            "Define clear acceptance criteria to ensure verification is straightforward.");
    }

    private static ImmutableArray<string> BuildMediumRecommendations(TaskComplexityMetrics metrics)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        builder.Add("This task has some complexity; plan execution steps carefully.");
        builder.Add("Consider incremental checkpoints to validate progress and correctness.");
        if (metrics.DependenciesCount > 0)
        {
            builder.Add("Double-check dependency outputs before proceeding.");
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> BuildHighRecommendations(TaskComplexityMetrics metrics)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        builder.Add("This task is high complexity; invest time in analysis before execution.");
        builder.Add("Consider splitting into smaller tasks with clear interfaces.");
        builder.Add("Establish milestones to track progress and quality.");
        if (metrics.DependenciesCount > TaskComplexityThresholds.DependenciesMedium)
        {
            builder.Add("Create a dependency map to avoid execution order issues.");
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> BuildVeryHighRecommendations(TaskComplexityMetrics metrics)
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        builder.Add("WARNING: This task has very high complexity; split it into multiple tasks.");
        builder.Add("Perform detailed analysis and define clear boundaries for each subtask.");
        builder.Add("Assess risks and define mitigation strategies before execution.");
        builder.Add("Establish explicit testing and verification standards for each output.");
        if (metrics.DescriptionLength >= TaskComplexityThresholds.DescriptionVeryHigh)
        {
            builder.Add("Summarize key requirements into a structured execution checklist.");
        }

        if (metrics.DependenciesCount >= TaskComplexityThresholds.DependenciesHigh)
        {
            builder.Add("Review dependencies to ensure task boundaries are reasonable.");
        }

        return builder.ToImmutable();
    }
}

internal static class TaskComplexityLevelFormatter
{
    public static string ToLabel(TaskComplexityLevel level)
    {
        return level switch
        {
            TaskComplexityLevel.Medium => "MEDIUM",
            TaskComplexityLevel.High => "HIGH",
            TaskComplexityLevel.VeryHigh => "VERY_HIGH",
            _ => "LOW"
        };
    }
}

internal static class TaskStatusFormatter
{
    private static readonly IReadOnlyDictionary<string, TaskStatus> StatusMap =
        new Dictionary<string, TaskStatus>(StringComparer.OrdinalIgnoreCase)
        {
            ["pending"] = TaskStatus.Pending,
            ["in_progress"] = TaskStatus.InProgress,
            ["inprogress"] = TaskStatus.InProgress,
            ["completed"] = TaskStatus.Completed,
            ["blocked"] = TaskStatus.Blocked
        };

    public static string ToSerializedValue(TaskStatus status)
    {
        return status switch
        {
            TaskStatus.InProgress => "in_progress",
            TaskStatus.Completed => "completed",
            TaskStatus.Blocked => "blocked",
            _ => "pending"
        };
    }

    public static TaskStatus? ParseFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return StatusMap.TryGetValue(value.Trim(), out var status) ? status : null;
    }
}

internal static class TaskDateFormatter
{
    public static string Format(DateTimeOffset value)
    {
        return value.ToString("f", CultureInfo.InvariantCulture);
    }
}

internal sealed class RelatedFilesSummaryBuilder
{
    public string BuildSummary(ImmutableArray<RelatedFile> relatedFiles)
    {
        if (relatedFiles.IsDefaultOrEmpty)
        {
            return "The current task has no associated files.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"## Related Files Summary (total {relatedFiles.Length} files)");
        builder.AppendLine();

        foreach (var file in relatedFiles)
        {
            builder.AppendLine(FormatFileLine(file));
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatFileLine(RelatedFile file)
    {
        var description = string.IsNullOrWhiteSpace(file.Description) ? string.Empty : $": {file.Description}";
        var range = FormatLineRange(file.LineStart, file.LineEnd);
        return $"- `{file.Path}` ({file.Type}){description}{range}";
    }

    private static string FormatLineRange(int? start, int? end)
    {
        if (!start.HasValue || !end.HasValue)
        {
            return string.Empty;
        }

        return $" [lines {start}-{end}]";
    }
}
