using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Config;
using Mcp.TaskAndResearch.Data;
using Mcp.TaskAndResearch.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using TaskStatus = Mcp.TaskAndResearch.Data.TaskStatus;

namespace Mcp.TaskAndResearch.Tests.Data;

/// <summary>
/// Tests for Result<T> and Maybe<T> patterns in the data layer.
/// Ensures proper error handling and railway-oriented programming patterns.
/// </summary>
public class ResultPatternTests
{
    #region TaskStore Result Pattern Tests

    [Fact]
    public async Task CreateAsync_Success_ReturnsResultWithTaskItem()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var result = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Test Task",
            Description = "Test Description"
        });

        Assert.True(result.IsSuccess, "CreateAsync should succeed");
        Assert.Equal("Test Task", result.Value.Name);
        Assert.Equal("Test Description", result.Value.Description);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTask_ReturnsMaybeWithValue()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var created = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Test Task",
            Description = "Test Description"
        });

        var maybe = await stores.TaskStore.GetByIdAsync(created.Value.Id);

        Assert.True(maybe.HasValue, "GetByIdAsync should return a value for existing task");
        Assert.Equal(created.Value.Id, maybe.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentTask_ReturnsMaybeWithNoValue()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var maybe = await stores.TaskStore.GetByIdAsync("non-existent-id");

        Assert.True(maybe.HasNoValue, "GetByIdAsync should return no value for non-existent task");
    }

    [Fact]
    public async Task UpdateAsync_ExistingTask_ReturnsResultSuccess()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var created = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Original",
            Description = "Original Description"
        });

        var result = await stores.TaskStore.UpdateAsync(created.Value.Id, new TaskUpdateRequest
        {
            Name = "Updated",
            Description = "Updated Description"
        });

        Assert.True(result.IsSuccess, "UpdateAsync should succeed for existing task");
        Assert.Equal("Updated", result.Value.Name);
        Assert.Equal("Updated Description", result.Value.Description);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentTask_ReturnsResultFailure()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var result = await stores.TaskStore.UpdateAsync("non-existent-id", new TaskUpdateRequest
        {
            Name = "Updated"
        });

        Assert.True(result.IsFailure, "UpdateAsync should fail for non-existent task");
        Assert.Contains("not found", result.Error.ToLowerInvariant());
    }

    [Fact]
    public async Task DeleteAsync_ExistingTask_ReturnsResultSuccess()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var created = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "To Delete",
            Description = "Will be deleted"
        });

        var result = await stores.TaskStore.DeleteAsync(created.Value.Id);

        Assert.True(result.IsSuccess, "DeleteAsync should succeed for existing task");

        var maybe = await stores.TaskStore.GetByIdAsync(created.Value.Id);
        Assert.True(maybe.HasNoValue, "Task should not exist after deletion");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentTask_ReturnsResultFailure()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var result = await stores.TaskStore.DeleteAsync("non-existent-id");

        Assert.True(result.IsFailure, "DeleteAsync should fail for non-existent task");
        Assert.Contains("not found", result.Error.ToLowerInvariant());
    }

    [Fact]
    public async Task ClearAllAsync_Success_ReturnsResultWithClearResult()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Task 1",
            Description = "First task"
        });

        await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Task 2",
            Description = "Second task"
        });

        var result = await stores.TaskStore.ClearAllAsync();

        Assert.True(result.IsSuccess, "ClearAllAsync should succeed");
        Assert.True(result.Value.Success, "ClearAll should report success");
    }

    [Fact]
    public async Task ClearAllAsync_BackupFails_ReturnsFailureWithoutDeletingTasks()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        // Create some tasks
        await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Task 1",
            Description = "First task"
        });

        await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Task 2",
            Description = "Second task"
        });

        // Make memory directory read-only to simulate backup failure
        var memoryDir = Path.Combine(temp.Path, "memory");
        if (Directory.Exists(memoryDir))
        {
            var dirInfo = new DirectoryInfo(memoryDir);
            dirInfo.Attributes |= FileAttributes.ReadOnly;

            try
            {
                var result = await stores.TaskStore.ClearAllAsync();

                // Should fail due to backup failure
                Assert.True(result.IsFailure, "ClearAllAsync should fail when backup fails");
                Assert.Contains("backup", result.Error.ToLowerInvariant());

                // Tasks should NOT be deleted
                var remainingTasksResult = await stores.TaskStore.GetAllAsync();
                Assert.True(remainingTasksResult.IsSuccess);
                Assert.True(remainingTasksResult.Value.Length >= 2, "Tasks should not be deleted when backup fails");
            }
            finally
            {
                // Restore directory permissions for cleanup
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }

    #endregion

    #region MemoryStore Result Pattern Tests

    [Fact]
    public async Task WriteSnapshotAsync_Success_ReturnsResultWithFilename()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var tasks = ImmutableArray.Create(CreateSnapshotTask("Task 1"), CreateSnapshotTask("Task 2"));

        var result = await stores.MemoryStore.WriteSnapshotAsync(tasks);

        Assert.True(result.IsSuccess, "WriteSnapshotAsync should succeed");
        Assert.NotEmpty(result.Value);
    }

    [Fact]
    public async Task ReadAllSnapshotsAsync_Success_ReturnsResultWithTasks()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var tasks = ImmutableArray.Create(CreateSnapshotTask("Task 1"), CreateSnapshotTask("Task 2"));
        await stores.MemoryStore.WriteSnapshotAsync(tasks);

        var result = await stores.MemoryStore.ReadAllSnapshotsAsync();

        Assert.True(result.IsSuccess, "ReadAllSnapshotsAsync should succeed");
        Assert.True(result.Value.Length >= 2, "Should read at least 2 tasks");
    }

    #endregion

    #region RulesStore Maybe Pattern Tests

    [Fact]
    public async Task WriteAsync_Success_ReturnsResultSuccess()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        var result = await stores.RulesStore.WriteAsync("Test content");

        Assert.True(result.IsSuccess, "WriteAsync should succeed");
    }

    [Fact]
    public async Task ReadAsync_AfterWrite_ReturnsMaybeWithValue()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        await stores.RulesStore.WriteAsync("Test rules content");
        var maybe = await stores.RulesStore.ReadAsync();

        Assert.True(maybe.HasValue, "ReadAsync should return value after write");
        Assert.Contains("Test rules content", maybe.Value);
    }

    #endregion

    #region TaskSearchService Result Pattern Tests

    [Fact]
    public async Task SearchAsync_Success_ReturnsResultWithSearchResult()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);
        var stores = CreateStores();

        await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Searchable Task",
            Description = "This is a searchable description"
        });

        var result = await stores.SearchService.SearchAsync("searchable", page: 1, pageSize: 10, isId: false);

        Assert.True(result.IsSuccess, "SearchAsync should succeed");
        Assert.True(result.Value.Tasks.Length > 0, "Should find at least one task");
    }

    #endregion

    #region Helper Methods

    private static EnvironmentScope CreateEnvScope(string tempPath)
    {
        return new EnvironmentScope(new Dictionary<string, string?>
        {
            ["TASK_MEMORY_DIR"] = tempPath,
            ["PROJECT_RULES_FILE"] = Path.Combine(tempPath, "rules.md")
        });
    }

    private static (TaskStore TaskStore, MemoryStore MemoryStore, RulesStore RulesStore, TaskSearchService SearchService) CreateStores()
    {
        var resolver = new PathResolver(new WorkspaceRootStore(), new ConfigReader(), NullLogger<PathResolver>.Instance);
        var pathProvider = new DataPathProvider(resolver);
        var memoryStore = new MemoryStore(pathProvider);
        var taskStore = new TaskStore(pathProvider, memoryStore);
        var rulesStore = new RulesStore(pathProvider);
        var searchService = new TaskSearchService(taskStore, memoryStore);
        return (taskStore, memoryStore, rulesStore, searchService);
    }

    private static TaskItem CreateSnapshotTask(string name)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = $"Description for {name}",
            Status = TaskStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
