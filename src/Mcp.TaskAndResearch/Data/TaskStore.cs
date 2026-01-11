using System.Collections.Immutable;
using System.Text.Json;

namespace Mcp.TaskAndResearch.Data;

internal sealed class TaskStore : ITaskReader
{
    private readonly DataPathProvider _pathProvider;
    private readonly MemoryStore _memoryStore;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Event raised when tasks are created, updated, deleted, or cleared.
    /// </summary>
    public event Action<TaskChangeEventArgs>? OnTaskChanged;

    public TaskStore(DataPathProvider pathProvider, MemoryStore memoryStore, TimeProvider? timeProvider = null)
    {
        _pathProvider = pathProvider;
        _memoryStore = memoryStore;
        _jsonOptions = JsonSettings.Default;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ImmutableArray<TaskItem>> GetAllAsync()
    {
        var document = await ReadDocumentAsync().ConfigureAwait(false);
        return document.Tasks;
    }

    public async Task<TaskItem?> GetByIdAsync(string taskId)
    {
        var document = await ReadDocumentAsync().ConfigureAwait(false);
        return document.Tasks.FirstOrDefault(task => task.Id == taskId);
    }

    public async Task<TaskItem> CreateAsync(TaskCreateRequest request)
    {
        var document = await ReadDocumentAsync().ConfigureAwait(false);
        var now = _timeProvider.GetLocalNow();

        var task = new TaskItem
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            Notes = request.Notes,
            Status = TaskStatus.Pending,
            Dependencies = ToDependencies(request.Dependencies),
            CreatedAt = now,
            UpdatedAt = now,
            RelatedFiles = request.RelatedFiles,
            AnalysisResult = request.AnalysisResult,
            Agent = request.Agent,
            ImplementationGuide = request.ImplementationGuide,
            VerificationCriteria = request.VerificationCriteria
        };

        var updatedDocument = document with
        {
            Tasks = document.Tasks.Add(task)
        };

        await WriteDocumentAsync(updatedDocument).ConfigureAwait(false);
        OnTaskChanged?.Invoke(TaskChangeEventArgs.Created(task));
        return task;
    }

    public async Task<TaskItem?> UpdateAsync(string taskId, TaskUpdateRequest request)
    {
        var document = await ReadDocumentAsync().ConfigureAwait(false);
        var index = FindTaskIndex(document.Tasks, taskId);
        if (index < 0)
        {
            return null;
        }

        var now = _timeProvider.GetLocalNow();
        var updated = ApplyUpdates(document.Tasks[index], request, now);
        var updatedDocument = document with
        {
            Tasks = document.Tasks.SetItem(index, updated)
        };

        await WriteDocumentAsync(updatedDocument).ConfigureAwait(false);
        OnTaskChanged?.Invoke(TaskChangeEventArgs.Updated(updated));
        return updated;
    }

    public async Task<bool> DeleteAsync(string taskId)
    {
        var document = await ReadDocumentAsync().ConfigureAwait(false);
        var index = FindTaskIndex(document.Tasks, taskId);
        if (index < 0)
        {
            return false;
        }

        var deletedTask = document.Tasks[index];
        var updatedDocument = document with
        {
            Tasks = document.Tasks.RemoveAt(index)
        };

        await WriteDocumentAsync(updatedDocument).ConfigureAwait(false);
        OnTaskChanged?.Invoke(TaskChangeEventArgs.Deleted(deletedTask));
        return true;
    }

    public async Task<ClearAllResult> ClearAllAsync()
    {
        var document = await ReadDocumentAsync().ConfigureAwait(false);
        if (document.Tasks.IsDefaultOrEmpty)
        {
            return ClearAllResult.Empty;
        }

        var completedTasks = document.Tasks.Where(task => task.Status == TaskStatus.Completed).ToImmutableArray();
        var backupFile = await _memoryStore.WriteSnapshotAsync(completedTasks).ConfigureAwait(false);

        await WriteDocumentAsync(TaskDocument.Empty).ConfigureAwait(false);
        OnTaskChanged?.Invoke(TaskChangeEventArgs.Cleared());

        return new ClearAllResult(true, "Tasks cleared.", backupFile);
    }

    private async Task<TaskDocument> ReadDocumentAsync()
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        if (!File.Exists(paths.TasksFilePath))
        {
            await WriteDocumentAsync(TaskDocument.Empty).ConfigureAwait(false);
        }

        await using var stream = File.OpenRead(paths.TasksFilePath);
        var document = await JsonSerializer.DeserializeAsync<TaskDocument>(stream, _jsonOptions).ConfigureAwait(false);
        return document ?? TaskDocument.Empty;
    }

    private async Task WriteDocumentAsync(TaskDocument document)
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await using var stream = File.Create(paths.TasksFilePath);
        await JsonSerializer.SerializeAsync(stream, document, _jsonOptions).ConfigureAwait(false);
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static int FindTaskIndex(ImmutableArray<TaskItem> tasks, string taskId)
    {
        for (var i = 0; i < tasks.Length; i++)
        {
            if (tasks[i].Id == taskId)
            {
                return i;
            }
        }

        return -1;
    }

    private static TaskItem ApplyUpdates(TaskItem existing, TaskUpdateRequest request, DateTimeOffset now)
    {
        var status = request.Status ?? existing.Status;
        DateTimeOffset? completedAt = status == TaskStatus.Completed ? existing.CompletedAt ?? now : null;

        return existing with
        {
            Name = request.Name ?? existing.Name,
            Description = request.Description ?? existing.Description,
            Notes = request.Notes ?? existing.Notes,
            Status = status,
            Dependencies = request.Dependencies is null ? existing.Dependencies : ToDependencies(request.Dependencies.Value),
            RelatedFiles = request.RelatedFiles ?? existing.RelatedFiles,
            Summary = request.Summary ?? existing.Summary,
            AnalysisResult = request.AnalysisResult ?? existing.AnalysisResult,
            Agent = request.Agent ?? existing.Agent,
            ImplementationGuide = request.ImplementationGuide ?? existing.ImplementationGuide,
            VerificationCriteria = request.VerificationCriteria ?? existing.VerificationCriteria,
            UpdatedAt = now,
            CompletedAt = completedAt
        };
    }

    private static ImmutableArray<TaskDependency> ToDependencies(ImmutableArray<string> dependencyIds)
    {
        if (dependencyIds.IsDefaultOrEmpty)
        {
            return ImmutableArray<TaskDependency>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<TaskDependency>();
        foreach (var dependencyId in dependencyIds)
        {
            if (!string.IsNullOrWhiteSpace(dependencyId))
            {
                builder.Add(new TaskDependency { TaskId = dependencyId });
            }
        }

        return builder.ToImmutable();
    }
}

internal sealed record ClearAllResult(bool Success, string Message, string? BackupFile)
{
    public static ClearAllResult Empty { get; } = new(true, "No tasks to clear.", null);
}
