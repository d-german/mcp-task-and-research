using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Mcp.TaskAndResearch.Data;
using Mcp.TaskAndResearch.Tests.TestSupport;
using TaskStatus = Mcp.TaskAndResearch.Data.TaskStatus;

namespace Mcp.TaskAndResearch.Tests.Data;

public sealed class TaskStorageTests
{
    [Fact]
    public async Task Crud_CreatesUpdatesAndDeletesTasks()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var stores = CreateStores();
        var createResult = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Initial",
            Description = "First task"
        });
        Assert.True(createResult.IsSuccess);
        var created = createResult.Value;

        var loaded = await stores.TaskStore.GetByIdAsync(created.Id);
        Assert.True(loaded.HasValue);

        var updateResult = await stores.TaskStore.UpdateAsync(created.Id, new TaskUpdateRequest
        {
            Name = "Updated",
            Status = TaskStatus.InProgress
        });

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Updated", updateResult.Value.Name);

        var deleted = await stores.TaskStore.DeleteAsync(created.Id);
        Assert.True(deleted.IsSuccess);

        var missing = await stores.TaskStore.GetByIdAsync(created.Id);
        Assert.True(missing.HasNoValue);
    }

    [Fact]
    public async Task ClearAll_ArchivesCompletedTasks()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var stores = CreateStores();
        var completedResult = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Done",
            Description = "Completed task"
        });
        await stores.TaskStore.UpdateAsync(completedResult.Value.Id, new TaskUpdateRequest
        {
            Status = TaskStatus.Completed
        });

        await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Pending",
            Description = "Still open"
        });

        var clearResult = await stores.TaskStore.ClearAllAsync();
        Assert.True(clearResult.IsSuccess);
        Assert.True(clearResult.Value.Success);

        var remainingResult = await stores.TaskStore.GetAllAsync();
        Assert.True(remainingResult.IsSuccess);
        Assert.Empty(remainingResult.Value);

        var memoryResult = await stores.MemoryStore.ReadAllSnapshotsAsync();
        Assert.True(memoryResult.IsSuccess);
        Assert.Single(memoryResult.Value);
        Assert.Equal(TaskStatus.Completed, memoryResult.Value[0].Status);
    }

    [Fact]
    public async Task Search_IncludesMemoryAndCurrentTasks()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var stores = CreateStores();
        var currentResult = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Alpha task",
            Description = "Current item"
        });
        var current = currentResult.Value;

        var snapshotTask = CreateSnapshotTask("memory-1", "Beta task", TaskStatus.Completed);
        await stores.MemoryStore.WriteSnapshotAsync(ImmutableArray.Create(snapshotTask));

        var searchResult = await stores.SearchService.SearchAsync("task", false, 1, 10);
        Assert.True(searchResult.IsSuccess);
        Assert.Equal(2, searchResult.Value.Pagination.TotalResults);

        var idSearchResult = await stores.SearchService.SearchAsync(current.Id, true, 1, 10);
        Assert.True(idSearchResult.IsSuccess);
        Assert.Single(idSearchResult.Value.Tasks);
        Assert.Equal(current.Id, idSearchResult.Value.Tasks[0].Id);
    }

    private static TaskItem CreateSnapshotTask(string id, string name, TaskStatus status)
    {
        var now = DateTimeOffset.UtcNow;
        return new TaskItem
        {
            Id = id,
            Name = name,
            Description = "Snapshot task",
            Status = status,
            Dependencies = ImmutableArray<TaskDependency>.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = status == TaskStatus.Completed ? now : null
        };
    }

    private static EnvironmentScope CreateEnvScope(string root)
    {
        var dataDir = Path.Combine(root, "data");
        return new EnvironmentScope(new Dictionary<string, string?>
        {
            ["DATA_DIR"] = dataDir,
            ["MCP_WORKSPACE_ROOT"] = root
        });
    }

    private static (TaskStore TaskStore, MemoryStore MemoryStore, TaskSearchService SearchService) CreateStores()
    {
        var resolver = new PathResolver(new WorkspaceRootStore(), new ConfigReader(), NullLogger<PathResolver>.Instance);
        var pathProvider = new DataPathProvider(resolver);
        var memoryStore = new MemoryStore(pathProvider);
        var taskStore = new TaskStore(pathProvider, memoryStore);
        var searchService = new TaskSearchService(taskStore, memoryStore);

        return (taskStore, memoryStore, searchService);
    }
}
