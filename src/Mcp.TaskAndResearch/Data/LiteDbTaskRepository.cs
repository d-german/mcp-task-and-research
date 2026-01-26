using System.Collections.Immutable;
using LiteDB;

namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// LiteDB implementation of <see cref="ITaskRepository"/>.
/// Provides ACID-compliant task storage without manual locking.
/// </summary>
internal sealed class LiteDbTaskRepository : ITaskRepository
{
    private readonly ILiteDbProvider _dbProvider;
    private readonly IMemoryRepository _memoryRepository;
    private readonly TimeProvider _timeProvider;
    private const string CollectionName = "tasks";

    /// <inheritdoc />
    public event Action<TaskChangeEventArgs>? OnTaskChanged;

    public LiteDbTaskRepository(
        ILiteDbProvider dbProvider,
        IMemoryRepository memoryRepository,
        TimeProvider? timeProvider = null)
    {
        _dbProvider = dbProvider;
        _memoryRepository = memoryRepository;
        _timeProvider = timeProvider ?? TimeProvider.System;
        EnsureIndexes();
    }

    private ILiteCollection<TaskItem> Tasks => _dbProvider.Database.GetCollection<TaskItem>(CollectionName);

    private void EnsureIndexes()
    {
        var tasks = Tasks;
        tasks.EnsureIndex(x => x.Id, unique: true);
        tasks.EnsureIndex(x => x.Name);
        tasks.EnsureIndex(x => x.Status);
    }

    /// <inheritdoc />
    public Task<ImmutableArray<TaskItem>> GetAllAsync()
    {
        var tasks = Tasks.FindAll()
            .OrderBy(t => t.CreatedAt)
            .ToImmutableArray();
        return Task.FromResult(tasks);
    }

    /// <inheritdoc />
    public Task<TaskItem?> GetByIdAsync(string taskId)
    {
        var task = Tasks.FindById(taskId);
        return Task.FromResult<TaskItem?>(task);
    }

    /// <inheritdoc />
    public Task<TaskItem> CreateAsync(TaskCreateRequest request)
    {
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

        Tasks.Insert(task);
        OnTaskChanged?.Invoke(TaskChangeEventArgs.Created(task));
        return Task.FromResult(task);
    }

    /// <inheritdoc />
    public Task<TaskItem?> UpdateAsync(string taskId, TaskUpdateRequest request)
    {
        var existing = Tasks.FindById(taskId);
        if (existing is null)
        {
            return Task.FromResult<TaskItem?>(null);
        }

        var now = _timeProvider.GetLocalNow();
        var updated = ApplyUpdates(existing, request, now);

        Tasks.Update(updated);
        OnTaskChanged?.Invoke(TaskChangeEventArgs.Updated(updated));
        return Task.FromResult<TaskItem?>(updated);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string taskId)
    {
        var existing = Tasks.FindById(taskId);
        if (existing is null)
        {
            return Task.FromResult(false);
        }

        var deleted = Tasks.Delete(taskId);
        if (deleted)
        {
            OnTaskChanged?.Invoke(TaskChangeEventArgs.Deleted(existing));
        }

        return Task.FromResult(deleted);
    }

    /// <inheritdoc />
    public async Task<ClearAllResult> ClearAllAsync()
    {
        var allTasks = Tasks.FindAll().ToList();
        if (allTasks.Count == 0)
        {
            return ClearAllResult.Empty;
        }

        var completedTasks = allTasks
            .Where(task => task.Status == TaskStatus.Completed)
            .ToImmutableArray();

        var backupFile = await _memoryRepository.WriteSnapshotAsync(completedTasks).ConfigureAwait(false);

        Tasks.DeleteAll();
        OnTaskChanged?.Invoke(TaskChangeEventArgs.Cleared());

        return new ClearAllResult(true, "Tasks cleared.", backupFile);
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
}
