using System.Collections.Immutable;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Extensions;

namespace Mcp.TaskAndResearch.Data;

internal sealed class TaskStore : ITaskReader
{
    private readonly DataPathProvider _pathProvider;
    private readonly MemoryStore _memoryStore;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Semaphore to serialize file access within this process.
    /// This prevents race conditions when multiple MCP tool calls
    /// attempt to read/write the tasks file concurrently.
    /// </summary>
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    /// <summary>
    /// Maximum number of retry attempts for file operations.
    /// </summary>
    private const int MaxRetries = 5;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff.
    /// </summary>
    private const int BaseDelayMs = 100;

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

    public async Task<Result<ImmutableArray<TaskItem>>> GetAllAsync()
    {
        var documentResult = await ReadDocumentAsync().ConfigureAwait(false);
        return documentResult.IsSuccess 
            ? Result.Success(documentResult.Value.Tasks) 
            : Result.Failure<ImmutableArray<TaskItem>>(documentResult.Error);
    }

    public async Task<Maybe<TaskItem>> GetByIdAsync(string taskId)
    {
        var documentResult = await ReadDocumentAsync().ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Maybe<TaskItem>.None;
        }

        var task = documentResult.Value.Tasks.FirstOrDefault(t => t.Id == taskId);
        return task.ToMaybe();
    }

    public async Task<Result<TaskItem>> CreateAsync(TaskCreateRequest request)
    {
        var documentResult = await ReadDocumentAsync().ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure<TaskItem>($"Failed to read task document: {documentResult.Error}");
        }

        var document = documentResult.Value;
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

        var writeResult = await WriteDocumentAsync(updatedDocument).ConfigureAwait(false);
        if (writeResult.IsFailure)
        {
            return Result.Failure<TaskItem>($"Failed to save task: {writeResult.Error}");
        }

        OnTaskChanged?.Invoke(TaskChangeEventArgs.Created(task));
        return Result.Success(task);
    }

    public async Task<Result<TaskItem>> UpdateAsync(string taskId, TaskUpdateRequest request)
    {
        var documentResult = await ReadDocumentAsync().ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure<TaskItem>($"Failed to read task document: {documentResult.Error}");
        }

        var document = documentResult.Value;
        var index = FindTaskIndex(document.Tasks, taskId);
        if (index < 0)
        {
            return Result.Failure<TaskItem>($"Task not found: {taskId}");
        }

        var now = _timeProvider.GetLocalNow();
        var updated = ApplyUpdates(document.Tasks[index], request, now);
        var updatedDocument = document with
        {
            Tasks = document.Tasks.SetItem(index, updated)
        };

        var writeResult = await WriteDocumentAsync(updatedDocument).ConfigureAwait(false);
        if (writeResult.IsFailure)
        {
            return Result.Failure<TaskItem>($"Failed to save task update: {writeResult.Error}");
        }

        OnTaskChanged?.Invoke(TaskChangeEventArgs.Updated(updated));
        return Result.Success(updated);
    }

    public async Task<Result> DeleteAsync(string taskId)
    {
        var documentResult = await ReadDocumentAsync().ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure($"Failed to read task document: {documentResult.Error}");
        }

        var document = documentResult.Value;
        var index = FindTaskIndex(document.Tasks, taskId);
        if (index < 0)
        {
            return Result.Failure($"Task not found: {taskId}");
        }

        var deletedTask = document.Tasks[index];
        var updatedDocument = document with
        {
            Tasks = document.Tasks.RemoveAt(index)
        };

        var writeResult = await WriteDocumentAsync(updatedDocument).ConfigureAwait(false);
        if (writeResult.IsFailure)
        {
            return Result.Failure($"Failed to delete task: {writeResult.Error}");
        }

        OnTaskChanged?.Invoke(TaskChangeEventArgs.Deleted(deletedTask));
        return Result.Success();
    }

    public async Task<Result<ClearAllResult>> ClearAllAsync()
    {
        var documentResult = await ReadDocumentAsync().ConfigureAwait(false);
        if (documentResult.IsFailure)
        {
            return Result.Failure<ClearAllResult>($"Failed to read task document: {documentResult.Error}");
        }

        var document = documentResult.Value;
        if (document.Tasks.IsDefaultOrEmpty)
        {
            return Result.Success(ClearAllResult.Empty);
        }

        var completedTasks = document.Tasks.Where(task => task.Status == TaskStatus.Completed).ToImmutableArray();
        
        // Backup completed tasks before clearing - fail the operation if backup fails to prevent data loss
        var backupResult = await _memoryStore.WriteSnapshotAsync(completedTasks).ConfigureAwait(false);
        if (backupResult.IsFailure)
        {
            return Result.Failure<ClearAllResult>($"Failed to backup completed tasks: {backupResult.Error}");
        }

        var writeResult = await WriteDocumentAsync(TaskDocument.Empty).ConfigureAwait(false);
        if (writeResult.IsFailure)
        {
            return Result.Failure<ClearAllResult>($"Failed to clear tasks: {writeResult.Error}");
        }
        
        var backupFile = backupResult.Value;

        OnTaskChanged?.Invoke(TaskChangeEventArgs.Cleared());
        return Result.Success(new ClearAllResult(true, "Tasks cleared.", backupFile));
    }

    private async Task<Result<TaskDocument>> ReadDocumentAsync()
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await FileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(paths.TasksFilePath))
            {
                await WriteDocumentCoreAsync(paths.TasksFilePath, TaskDocument.Empty).ConfigureAwait(false);
            }

            return await ReadWithRetryAsync(paths.TasksFilePath).ConfigureAwait(false);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task<Result> WriteDocumentAsync(TaskDocument document)
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await FileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await WriteWithRetryAsync(paths.TasksFilePath, document).ConfigureAwait(false);
        }
        finally
        {
            FileLock.Release();
        }
    }

    /// <summary>
    /// Reads the document with retry logic for handling temporary file locks.
    /// </summary>
    private async Task<Result<TaskDocument>> ReadWithRetryAsync(string filePath)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var readResult = await AsyncResultExtensions.TryAsync(async () =>
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                var document = await JsonSerializer.DeserializeAsync<TaskDocument>(stream, _jsonOptions).ConfigureAwait(false);
                return document ?? TaskDocument.Empty;
            }).ConfigureAwait(false);

            if (readResult.IsSuccess)
            {
                return readResult;
            }

            if (attempt < MaxRetries - 1)
            {
                var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        // Final attempt
        return await AsyncResultExtensions.TryAsync(async () =>
        {
            await using var finalStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            return await JsonSerializer.DeserializeAsync<TaskDocument>(finalStream, _jsonOptions).ConfigureAwait(false) ?? TaskDocument.Empty;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the document with retry logic and atomic write pattern.
    /// Uses temp file + rename to prevent corruption.
    /// </summary>
    private async Task<Result> WriteWithRetryAsync(string filePath, TaskDocument document)
    {
        var tempPath = filePath + ".tmp." + Guid.NewGuid().ToString("N");

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var writeResult = await AsyncResultExtensions.TryAsync(async () =>
            {
                await WriteDocumentCoreAsync(tempPath, document).ConfigureAwait(false);
                File.Move(tempPath, filePath, overwrite: true);
            }).ConfigureAwait(false);

            if (writeResult.IsSuccess)
            {
                return writeResult;
            }

            // Clean up temp file if it exists
            TryDeleteFile(tempPath);

            if (attempt < MaxRetries - 1)
            {
                var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        // Final attempt
        return await AsyncResultExtensions.TryAsync(async () =>
        {
            await WriteDocumentCoreAsync(tempPath, document).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Core write operation without retry logic.
    /// </summary>
    private static async Task WriteDocumentCoreAsync(string filePath, TaskDocument document)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        await JsonSerializer.SerializeAsync(stream, document, JsonSettings.Default).ConfigureAwait(false);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
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
