using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Config;
using Mcp.TaskAndResearch.Data;
using Mcp.TaskAndResearch.Prompts;
using Mcp.TaskAndResearch.Tests.TestSupport;
using Mcp.TaskAndResearch.Tools.Task;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TaskStatus = Mcp.TaskAndResearch.Data.TaskStatus;

namespace Mcp.TaskAndResearch.Tests.Tools;

public sealed class TaskToolsTests
{
    [Fact]
    public void AnalyzeTask_BuildsPromptWithInputs()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new AnalyzeTaskPromptBuilder(context.Loader);

        var prompt = TaskTools.AnalyzeTask(builder, "Summary text", "Initial concept", "Previous analysis");

        Assert.Contains("Summary text", prompt);
        Assert.Contains("Initial concept", prompt);
        Assert.Contains("Previous analysis", prompt);
    }

    [Fact]
    public void ReflectTask_BuildsPromptWithInputs()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new ReflectTaskPromptBuilder(context.Loader);

        var prompt = TaskTools.ReflectTask(builder, "Summary text", "Detailed analysis");

        Assert.Contains("Summary text", prompt);
        Assert.Contains("Detailed analysis", prompt);
    }

    [Fact]
    public async Task PlanTask_BuildsPromptWithPaths()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new PlanTaskPromptBuilder(context.Loader);

        var prompt = await TaskTools.PlanTask(
            builder,
            context.TaskStore,
            context.PathProvider,
            "Plan something",
            "Must be reliable",
            existingTasksReference: false);

        Assert.Contains("Plan something", prompt);
        Assert.Contains(context.PathProvider.GetPaths().MemoryDirectory, prompt);
    }

    [Fact]
    public async Task SplitTasks_CreatesTasksAndBuildsPrompt()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new SplitTasksPromptBuilder(context.Loader);

        var inputs = new[]
        {
            new TaskInput { Name = "Task A", Description = "First task" },
            new TaskInput { Name = "Task B", Description = "Second task", Dependencies = new[] { "Task A" } }
        };

        var prompt = await TaskTools.SplitTasks(builder, context.BatchService, "append", inputs, "Global analysis");

        var tasksResult = await context.TaskStore.GetAllAsync();
        Assert.True(tasksResult.IsSuccess);
        var tasks = tasksResult.Value;
        Assert.Equal(2, tasks.Length);
        Assert.Contains("Task A", prompt);
        Assert.Contains("Task B", prompt);
    }

    [Fact]
    public async Task ListTasks_BuildsPromptWithTaskDetails()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new ListTasksPromptBuilder(context.Loader);

        await CreateTaskAsync(context.TaskStore, "List target", "List description");

        var prompt = await TaskTools.ListTasks(builder, context.TaskStore, "all");

        Assert.Contains("List target", prompt);
    }

    [Fact]
    public async Task QueryTask_BuildsPromptWithMatches()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new QueryTaskPromptBuilder(context.Loader);

        await CreateTaskAsync(context.TaskStore, "Alpha task", "Searchable task");

        var prompt = await TaskTools.QueryTask(builder, context.SearchService, "Alpha", false, 1, 5);

        Assert.Contains("Alpha task", prompt);
    }

    [Fact]
    public async Task GetTaskDetail_BuildsPromptForExistingTask()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new GetTaskDetailPromptBuilder(context.Loader);

        var task = await CreateTaskAsync(context.TaskStore, "Detail task", "Detail description");
        var prompt = await TaskTools.GetTaskDetail(builder, context.TaskStore, context.SearchService, task.Id);

        Assert.Contains(task.Id, prompt);
        Assert.Contains("Detail task", prompt);
    }

    [Fact]
    public async Task ExecuteTask_MarksInProgressAndBuildsPrompt()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new ExecuteTaskPromptBuilder(context.Loader);

        var task = await CreateTaskAsync(context.TaskStore, "Execute task", "Execute description");
        var prompt = await TaskTools.ExecuteTask(builder, context.WorkflowService, context.TaskStore, task.Id);

        var updated = await context.TaskStore.GetByIdAsync(task.Id);
        Assert.True(updated.HasValue);
        Assert.Equal(TaskStatus.InProgress, updated.Value.Status);
        Assert.Contains("Execute task", prompt);
    }

    [Fact]
    public async Task VerifyTask_CompletesTaskAndBuildsPrompt()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new VerifyTaskPromptBuilder(context.Loader);

        var task = await CreateTaskAsync(context.TaskStore, "Verify task", "Verify description");
        await context.TaskStore.UpdateAsync(task.Id, new TaskUpdateRequest { Status = TaskStatus.InProgress });

        var prompt = await TaskTools.VerifyTask(builder, context.WorkflowService, context.TaskStore, task.Id, 85, "All checks passed");

        var updated = await context.TaskStore.GetByIdAsync(task.Id);
        Assert.True(updated.HasValue);
        Assert.Equal(TaskStatus.Completed, updated.Value.Status);
        Assert.Equal("All checks passed", updated.Value.Summary);
        Assert.Contains("All checks passed", prompt);
    }

    [Fact]
    public async Task DeleteTask_RemovesTaskAndBuildsPrompt()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new DeleteTaskPromptBuilder(context.Loader);

        var task = await CreateTaskAsync(context.TaskStore, "Delete task", "Delete description");
        var prompt = await TaskTools.DeleteTask(builder, context.TaskStore, task.Id);

        var missing = await context.TaskStore.GetByIdAsync(task.Id);
        Assert.True(missing.HasNoValue);
        Assert.Contains(task.Id, prompt);
    }

    [Fact]
    public async Task ClearAllTasks_RemovesIncompleteTasks()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new ClearAllTasksPromptBuilder(context.Loader);

        await CreateTaskAsync(context.TaskStore, "Clear task", "Clear description");

        var prompt = await TaskTools.ClearAllTasks(builder, context.TaskStore, confirm: true);

        var tasksResult = await context.TaskStore.GetAllAsync();
        Assert.True(tasksResult.IsSuccess);
        Assert.Empty(tasksResult.Value);
        Assert.Contains("Clear All Tasks Result", prompt);
    }

    [Fact]
    public async Task UpdateTask_UpdatesTaskAndBuildsPrompt()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var context = CreateContext();
        var builder = new UpdateTaskPromptBuilder(context.Loader);

        var task = await CreateTaskAsync(context.TaskStore, "Original name", "Original description");
        var prompt = await TaskTools.UpdateTask(
            builder,
            context.TaskStore,
            task.Id,
            name: "Updated name",
            description: "Updated description");

        var updated = await context.TaskStore.GetByIdAsync(task.Id);
        Assert.True(updated.HasValue);
        Assert.Equal("Updated name", updated.Value.Name);
        Assert.Contains("Updated name", prompt);
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

    private static ToolTestContext CreateContext()
    {
        var resolver = new PathResolver(new WorkspaceRootStore(), new ConfigReader(), NullLogger<PathResolver>.Instance);
        var pathProvider = new DataPathProvider(resolver);
        var loader = new PromptTemplateLoader(resolver);
        var memoryStore = new MemoryStore(pathProvider);
        var taskStore = new TaskStore(pathProvider, memoryStore);
        var searchService = new TaskSearchService(taskStore, memoryStore);
        var planner = new TaskUpdatePlanner();
        var batchService = new TaskBatchService(taskStore, planner);
        var workflowService = new TaskWorkflowService(
            taskStore,
            new TaskComplexityAssessor(),
            new RelatedFilesSummaryBuilder());

        return new ToolTestContext(loader, pathProvider, taskStore, searchService, batchService, workflowService);
    }

    private static async Task<TaskItem> CreateTaskAsync(TaskStore taskStore, string name, string description)
    {
        var result = await taskStore.CreateAsync(new TaskCreateRequest
        {
            Name = name,
            Description = description
        }).ConfigureAwait(false);
        return result.Value;
    }

    private sealed record ToolTestContext(
        PromptTemplateLoader Loader,
        DataPathProvider PathProvider,
        TaskStore TaskStore,
        TaskSearchService SearchService,
        TaskBatchService BatchService,
        TaskWorkflowService WorkflowService);
}
