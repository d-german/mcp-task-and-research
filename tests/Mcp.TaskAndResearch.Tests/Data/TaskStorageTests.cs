using System.Collections.Immutable;
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
        var created = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Initial",
            Description = "First task"
        });

        var loaded = await stores.TaskStore.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);

        var updated = await stores.TaskStore.UpdateAsync(created.Id, new TaskUpdateRequest
        {
            Name = "Updated",
            Status = TaskStatus.InProgress
        });

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Name);

        var deleted = await stores.TaskStore.DeleteAsync(created.Id);
        Assert.True(deleted);

        var missing = await stores.TaskStore.GetByIdAsync(created.Id);
        Assert.Null(missing);
    }

    [Fact]
    public async Task ClearAll_ArchivesCompletedTasks()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var stores = CreateStores();
        var completed = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Done",
            Description = "Completed task"
        });
        await stores.TaskStore.UpdateAsync(completed.Id, new TaskUpdateRequest
        {
            Status = TaskStatus.Completed
        });

        await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Pending",
            Description = "Still open"
        });

        var result = await stores.TaskStore.ClearAllAsync();
        Assert.True(result.Success);

        var remaining = await stores.TaskStore.GetAllAsync();
        Assert.Empty(remaining);

        var memoryTasks = await stores.MemoryStore.ReadAllSnapshotsAsync();
        Assert.Single(memoryTasks);
        Assert.Equal(TaskStatus.Completed, memoryTasks[0].Status);
    }

    [Fact]
    public async Task Search_IncludesMemoryAndCurrentTasks()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var stores = CreateStores();
        var current = await stores.TaskStore.CreateAsync(new TaskCreateRequest
        {
            Name = "Alpha task",
            Description = "Current item"
        });

        var snapshotTask = CreateSnapshotTask("memory-1", "Beta task", TaskStatus.Completed);
        await stores.MemoryStore.WriteSnapshotAsync(ImmutableArray.Create(snapshotTask));

        var search = await stores.SearchService.SearchAsync("task", false, 1, 10);
        Assert.Equal(2, search.Pagination.TotalResults);

        var idSearch = await stores.SearchService.SearchAsync(current.Id, true, 1, 10);
        Assert.Single(idSearch.Tasks);
        Assert.Equal(current.Id, idSearch.Tasks[0].Id);
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
