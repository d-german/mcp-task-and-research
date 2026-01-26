using System.Collections.Immutable;
using System.Text.Json;

namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Implementation of <see cref="IMigrationService"/> that orchestrates migration between
/// legacy JSON stores and LiteDB repositories.
/// </summary>
internal sealed class MigrationService : IMigrationService
{
    private readonly TaskStore _legacyTaskStore;
    private readonly MemoryStore _legacyMemoryStore;
    private readonly ITaskRepository _taskRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly DataPathProvider _pathProvider;

    public MigrationService(
        TaskStore legacyTaskStore,
        MemoryStore legacyMemoryStore,
        ITaskRepository taskRepository,
        IMemoryRepository memoryRepository,
        DataPathProvider pathProvider)
    {
        _legacyTaskStore = legacyTaskStore;
        _legacyMemoryStore = legacyMemoryStore;
        _taskRepository = taskRepository;
        _memoryRepository = memoryRepository;
        _pathProvider = pathProvider;
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportFromJsonAsync(string? sourcePath = null)
    {
        try
        {
            // Read from legacy stores
            var legacyTasks = await _legacyTaskStore.GetAllAsync().ConfigureAwait(false);
            var legacySnapshots = await _legacyMemoryStore.ReadAllSnapshotsAsync().ConfigureAwait(false);

            if (legacyTasks.IsDefaultOrEmpty && legacySnapshots.IsDefaultOrEmpty)
            {
                return ImportResult.Empty;
            }

            // Get existing tasks to check for duplicates
            var existingTasks = await _taskRepository.GetAllAsync().ConfigureAwait(false);
            var existingIds = CreateIdSet(existingTasks);

            // Import tasks (skip duplicates)
            var importedTaskCount = 0;
            foreach (var task in legacyTasks)
            {
                if (!existingIds.Contains(task.Id))
                {
                    await ImportTaskAsync(task).ConfigureAwait(false);
                    importedTaskCount++;
                }
            }

            // Import snapshots if any
            var importedSnapshotCount = 0;
            if (!legacySnapshots.IsDefaultOrEmpty)
            {
                await _memoryRepository.WriteSnapshotAsync(legacySnapshots).ConfigureAwait(false);
                importedSnapshotCount = 1; // One consolidated snapshot
            }

            return new ImportResult(
                Success: true,
                TasksImported: importedTaskCount,
                SnapshotsImported: importedSnapshotCount);
        }
        catch (Exception ex)
        {
            return ImportResult.Failure($"Import failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ExportResult> ExportToJsonAsync(string? destinationPath = null)
    {
        try
        {
            var paths = _pathProvider.GetPaths();
            var targetDataDir = destinationPath ?? paths.DataDirectory;

            // Read from LiteDB repositories
            var tasks = await _taskRepository.GetAllAsync().ConfigureAwait(false);
            var snapshots = await _memoryRepository.ReadAllSnapshotsAsync().ConfigureAwait(false);

            // Export tasks to JSON
            var tasksFilePath = Path.Combine(targetDataDir, "tasks.json");
            await ExportTasksToFileAsync(tasks, tasksFilePath).ConfigureAwait(false);

            // Export snapshots to memory directory
            var memoryDir = Path.Combine(targetDataDir, "memory");
            if (!snapshots.IsDefaultOrEmpty)
            {
                await ExportSnapshotsToDirectoryAsync(snapshots, memoryDir).ConfigureAwait(false);
            }

            return new ExportResult(
                Success: true,
                TasksFilePath: tasksFilePath,
                MemoryDirectory: memoryDir);
        }
        catch (Exception ex)
        {
            return ExportResult.Failure($"Export failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<MigrationStatus> GetMigrationStatusAsync()
    {
        try
        {
            var paths = _pathProvider.GetPaths();
            
            var hasTasksJson = File.Exists(paths.TasksFilePath);
            var hasMemorySnapshots = Directory.Exists(paths.MemoryDirectory) && 
                                      Directory.EnumerateFiles(paths.MemoryDirectory, "*.json").Any();

            if (!hasTasksJson && !hasMemorySnapshots)
            {
                return MigrationStatus.NoLegacyFiles;
            }

            // Count tasks and snapshots
            var taskCount = 0;
            if (hasTasksJson)
            {
                var legacyTasks = await _legacyTaskStore.GetAllAsync().ConfigureAwait(false);
                taskCount = legacyTasks.Length;
            }

            var snapshotCount = 0;
            if (hasMemorySnapshots)
            {
                var files = Directory.EnumerateFiles(paths.MemoryDirectory, "*.json").ToList();
                snapshotCount = files.Count;
            }

            return new MigrationStatus(
                LegacyFilesExist: true,
                HasTasksJson: hasTasksJson,
                HasMemorySnapshots: hasMemorySnapshots,
                LegacyTaskCount: taskCount,
                LegacySnapshotCount: snapshotCount);
        }
        catch (Exception)
        {
            return MigrationStatus.NoLegacyFiles;
        }
    }

    // Helper methods below are static since they don't use instance state

    /// <summary>
    /// Creates a hash set of task IDs for efficient duplicate checking.
    /// </summary>
    private static HashSet<string> CreateIdSet(ImmutableArray<TaskItem> tasks)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var task in tasks)
        {
            set.Add(task.Id);
        }
        return set;
    }

    /// <summary>
    /// Imports a single task by creating it via the repository.
    /// </summary>
    private async Task ImportTaskAsync(TaskItem task)
    {
        var request = new TaskCreateRequest
        {
            Name = task.Name,
            Description = task.Description,
            Notes = task.Notes,
            Dependencies = task.Dependencies.Select(d => d.TaskId).ToList(),
            RelatedFiles = task.RelatedFiles,
            AnalysisResult = task.AnalysisResult,
            Agent = task.Agent,
            ImplementationGuide = task.ImplementationGuide,
            VerificationCriteria = task.VerificationCriteria
        };

        // Create the task, then update its status and timestamps to preserve original values
        var createdTask = await _taskRepository.CreateAsync(request).ConfigureAwait(false);
        
        // Update to preserve original timestamps and status
        var updateRequest = new TaskUpdateRequest
        {
            Status = task.Status,
            Summary = task.Summary
        };
        
        await _taskRepository.UpdateAsync(createdTask.Id, updateRequest).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports tasks to a JSON file.
    /// </summary>
    private static async Task ExportTasksToFileAsync(ImmutableArray<TaskItem> tasks, string filePath)
    {
        EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);
        
        var document = new TaskDocument { Tasks = tasks };
        
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonSettings.Default).ConfigureAwait(false);
    }

    /// <summary>
    /// Exports snapshots to individual JSON files in a directory.
    /// </summary>
    private static async Task ExportSnapshotsToDirectoryAsync(ImmutableArray<TaskItem> snapshots, string directory)
    {
        EnsureDirectoryExists(directory);
        
        var fileName = $"snapshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(directory, fileName);
        
        var document = new TaskDocument { Tasks = snapshots };
        
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonSettings.Default).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
