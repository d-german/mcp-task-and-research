using System.Collections.Immutable;
using System.Text.Json;

namespace Mcp.TaskAndResearch.Data;

internal sealed class TaskStore : ITaskRepository
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

    public async Task<ImmutableArray<TaskItem>> GetAllAsync()
    {
        var document = await ReadDocumentAsync().ConfigureAwait(false);
        return document.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .ToImmutableArray();
    }

    public async Task<TaskItem?> GetByIdAsync(string taskId)
    {
        var document = await ReadDocumentAsync().ConfigureAwait(false);
        return document.Tasks.FirstOrDefault(task => task.Id == taskId);
    }

    public async Task<TaskItem> CreateAsync(TaskCreateRequest request)
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await FileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var document = await ReadDocumentCoreAsync(paths.TasksFilePath).ConfigureAwait(false);
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

            await WriteWithRetryAsync(paths.TasksFilePath, updatedDocument).ConfigureAwait(false);
            OnTaskChanged?.Invoke(TaskChangeEventArgs.Created(task));
            return task;
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<TaskItem?> UpdateAsync(string taskId, TaskUpdateRequest request)
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await FileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var document = await ReadDocumentCoreAsync(paths.TasksFilePath).ConfigureAwait(false);
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

            await WriteWithRetryAsync(paths.TasksFilePath, updatedDocument).ConfigureAwait(false);
            OnTaskChanged?.Invoke(TaskChangeEventArgs.Updated(updated));
            return updated;
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<bool> DeleteAsync(string taskId)
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await FileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var document = await ReadDocumentCoreAsync(paths.TasksFilePath).ConfigureAwait(false);
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

            await WriteWithRetryAsync(paths.TasksFilePath, updatedDocument).ConfigureAwait(false);
            OnTaskChanged?.Invoke(TaskChangeEventArgs.Deleted(deletedTask));
            return true;
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<ClearAllResult> ClearAllAsync()
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await FileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var document = await ReadDocumentCoreAsync(paths.TasksFilePath).ConfigureAwait(false);
            if (document.Tasks.IsDefaultOrEmpty)
            {
                return ClearAllResult.Empty;
            }

            var completedTasks = document.Tasks.Where(task => task.Status == TaskStatus.Completed).ToImmutableArray();
            var backupFile = await _memoryStore.WriteSnapshotAsync(completedTasks).ConfigureAwait(false);

            await WriteWithRetryAsync(paths.TasksFilePath, TaskDocument.Empty).ConfigureAwait(false);
            OnTaskChanged?.Invoke(TaskChangeEventArgs.Cleared());

            return new ClearAllResult(true, "Tasks cleared.", backupFile);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task<TaskDocument> ReadDocumentAsync()
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await FileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await ReadDocumentCoreAsync(paths.TasksFilePath).ConfigureAwait(false);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task WriteDocumentAsync(TaskDocument document)
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        await FileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await WriteWithRetryAsync(paths.TasksFilePath, document).ConfigureAwait(false);
        }
        finally
        {
            FileLock.Release();
        }
    }


    /// <summary>
    /// Reads the document without acquiring the lock.
    /// Caller must already hold FileLock.
    /// </summary>
    private async Task<TaskDocument> ReadDocumentCoreAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            await WriteDocumentCoreAsync(filePath, TaskDocument.Empty).ConfigureAwait(false);
        }

        return await ReadWithRetryAsync(filePath).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the document with retry logic for handling temporary file locks.
    /// </summary>
    private async Task<TaskDocument> ReadWithRetryAsync(string filePath)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                var document = await JsonSerializer.DeserializeAsync<TaskDocument>(stream, _jsonOptions).ConfigureAwait(false);
                return document ?? TaskDocument.Empty;
            }
            catch (IOException) when (attempt < MaxRetries - 1)
            {
                var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        // Final attempt - let exception propagate if it fails
        await using var finalStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        return await JsonSerializer.DeserializeAsync<TaskDocument>(finalStream, _jsonOptions).ConfigureAwait(false) ?? TaskDocument.Empty;
    }

    /// <summary>
    /// Writes the document with retry logic and atomic write pattern.
    /// Uses temp file + rename to prevent corruption.
    /// </summary>
    private async Task WriteWithRetryAsync(string filePath, TaskDocument document)
    {
        var tempPath = filePath + ".tmp." + Guid.NewGuid().ToString("N");

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                await WriteDocumentCoreAsync(tempPath, document).ConfigureAwait(false);
                File.Move(tempPath, filePath, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < MaxRetries - 1)
            {
                // Clean up temp file if it exists
                TryDeleteFile(tempPath);
                var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        // Final attempt - let exception propagate if it fails
        await WriteDocumentCoreAsync(tempPath, document).ConfigureAwait(false);
        File.Move(tempPath, filePath, overwrite: true);
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
            Dependencies = request.Dependencies is null ? existing.Dependencies : ToDependencies(request.Dependencies),
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

    private static List<TaskDependency> ToDependencies(List<string> dependencyIds)
    {
        if (dependencyIds.Count == 0)
        {
            return [];
        }

        var result = new List<TaskDependency>();
        foreach (var dependencyId in dependencyIds)
        {
            if (!string.IsNullOrWhiteSpace(dependencyId))
            {
                result.Add(new TaskDependency { TaskId = dependencyId });
            }
        }

        return result;
    }
}

internal sealed record ClearAllResult(bool Success, string Message, string? BackupFile)
{
    public static ClearAllResult Empty { get; } = new(true, "No tasks to clear.", null);
}
