using System.Collections.Immutable;
using System.ComponentModel;
using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Data;
using ModelContextProtocol.Server;
using TaskStatus = Mcp.TaskAndResearch.Data.TaskStatus;

namespace Mcp.TaskAndResearch.Tools.Task;

[McpServerToolType]
internal static class TaskTools
{
    [McpServerTool(Name = "analyze_task")]
    [Description("Deeply analyze task requirements and propose a high-level approach.")]
    public static string AnalyzeTask(
        AnalyzeTaskPromptBuilder promptBuilder,
        [Description("Structured task summary.")] string summary,
        [Description("Initial analysis concept with pseudocode if needed.")] string initialConcept,
        [Description("Previous analysis from prior iteration.")] string? previousAnalysis = null)
    {
        return promptBuilder.Build(summary, initialConcept, previousAnalysis);
    }

    [McpServerTool(Name = "reflect_task")]
    [Description("Critically review analysis results and propose optimizations.")]
    public static string ReflectTask(
        ReflectTaskPromptBuilder promptBuilder,
        [Description("Summary consistent with the analysis stage.")] string summary,
        [Description("Full analysis results for reflection.")] string analysis)
    {
        return promptBuilder.Build(summary, analysis);
    }

    [McpServerTool(Name = "plan_task")]
    [Description("Plan tasks and construct a structured task list.")]
    public static async Task<string> PlanTask(
        PlanTaskPromptBuilder promptBuilder,
        TaskStore taskStore,
        DataPathProvider pathProvider,
        [Description("Full task description including goal and expected outcome.")] string description,
        [Description("Specific technical requirements or constraints.")] string? requirements = null,
        [Description("Whether to reference existing tasks as context.")] bool existingTasksReference = false)
    {
        var snapshot = await LoadPlanSnapshotAsync(taskStore, existingTasksReference).ConfigureAwait(false);
        var paths = pathProvider.GetPaths();
        var rulesPath = Path.GetFileName(paths.RulesFilePath);
        return promptBuilder.Build(
            description,
            requirements,
            existingTasksReference,
            snapshot.Completed,
            snapshot.Pending,
            paths.MemoryDirectory,
            rulesPath);
    }

    [McpServerTool(Name = "split_tasks")]
    [Description("Split a complex task into a structured set of smaller tasks.")]
    public static async Task<string> SplitTasks(
        SplitTasksPromptBuilder promptBuilder,
        TaskBatchService batchService,
        [Description("Task update mode: append, overwrite, selective, clearAllTasks.")] string updateMode,
        [Description("Structured task list to create or update.")] TaskInput[] tasks,
        [Description("Optional global analysis result.")] string? globalAnalysisResult = null)
    {
        var validationResult = TaskInputValidator.ValidateUniqueNames(tasks);
        if (validationResult.IsFailure)
        {
            return validationResult.Error;
        }

        var mode = TaskUpdateModeParser.Parse(updateMode);
        var batchResult = await batchService.ApplyAsync(mode, tasks.ToImmutableArray(), globalAnalysisResult)
            .ConfigureAwait(false);

        return batchResult.Match(
            result => promptBuilder.Build(updateMode, result.CreatedTasks, result.AllTasks),
            error => $"Error while splitting tasks: {error}");
    }

    [McpServerTool(Name = "list_tasks")]
    [Description("List tasks by status.")]
    public static async Task<string> ListTasks(
        ListTasksPromptBuilder promptBuilder,
        TaskStore taskStore,
        [Description("Status filter: all, pending, in_progress, completed.")] string status = "all")
    {
        var tasksResult = await taskStore.GetAllAsync().ConfigureAwait(false);
        if (tasksResult.IsFailure)
        {
            return tasksResult.Error;
        }

        var tasks = tasksResult.Value;
        var filterStatus = TaskStatusFormatter.ParseFilter(status);
        var filtered = filterStatus.HasValue
            ? tasks.Where(task => task.Status == filterStatus.Value).ToImmutableArray()
            : tasks;
        return promptBuilder.Build(status, tasks, filtered);
    }

    [McpServerTool(Name = "query_task")]
    [Description("Search tasks by keyword or ID with pagination.")]
    public static async Task<string> QueryTask(
        QueryTaskPromptBuilder promptBuilder,
        TaskSearchService searchService,
        [Description("Search query string or task ID.")] string query,
        [Description("True to treat the query as an exact ID search.")] bool isId = false,
        [Description("Page number.")] int page = 1,
        [Description("Number of items per page.")] int pageSize = 3)
    {
        var searchResult = await searchService.SearchAsync(query, isId, page, pageSize).ConfigureAwait(false);
        if (searchResult.IsFailure)
        {
            return $"Search failed: {searchResult.Error}";
        }

        var result = searchResult.Value;
        return promptBuilder.Build(
            query,
            result.Tasks,
            result.Pagination.TotalResults,
            result.Pagination.CurrentPage,
            pageSize,
            result.Pagination.TotalPages);
    }

    [McpServerTool(Name = "get_task_detail")]
    [Description("Retrieve full details for a specific task.")]
    public static async Task<string> GetTaskDetail(
        GetTaskDetailPromptBuilder promptBuilder,
        TaskStore taskStore,
        TaskSearchService searchService,
        [Description("Task ID to retrieve.")] string taskId)
    {
        var taskMaybe = await taskStore.GetByIdAsync(taskId).ConfigureAwait(false);
        
        if (taskMaybe.HasNoValue)
        {
            // Fallback to search
            var searchResult = await searchService.SearchAsync(taskId, true, 1, 1).ConfigureAwait(false);
            if (searchResult.IsSuccess && searchResult.Value.Tasks.Length > 0)
            {
                return promptBuilder.Build(taskId, searchResult.Value.Tasks[0], null);
            }
        }

        return promptBuilder.Build(taskId, taskMaybe.GetValueOrDefault(), null);
    }

    [McpServerTool(Name = "execute_task")]
    [Description("Mark a task as in progress and build an execution prompt.")]
    public static async Task<string> ExecuteTask(
        ExecuteTaskPromptBuilder promptBuilder,
        TaskWorkflowService workflowService,
        TaskStore taskStore,
        [Description("Task ID to execute.")] string taskId)
    {
        var taskMaybe = await taskStore.GetByIdAsync(taskId).ConfigureAwait(false);
        if (taskMaybe.HasNoValue)
        {
            return $"Task with ID `{taskId}` could not be found. Please confirm the ID and try again.";
        }

        var task = taskMaybe.Value;
        if (task.Status == TaskStatus.InProgress)
        {
            return $"Task \"{task.Name}\" (ID: `{taskId}`) is already in progress.";
        }

        if (task.Status == TaskStatus.Completed)
        {
            return $"Task \"{task.Name}\" (ID: `{taskId}`) is already completed. Delete and recreate it to execute again.";
        }

        var executionCheckResult = await workflowService.CanExecuteAsync(task).ConfigureAwait(false);
        if (executionCheckResult.IsFailure)
        {
            return $"Task \"{task.Name}\" (ID: `{taskId}`) check failed: {executionCheckResult.Error}";
        }
        
        var executionCheck = executionCheckResult.Value;
        if (!executionCheck.CanExecute)
        {
            var blockedBy = executionCheck.BlockedBy.IsDefaultOrEmpty
                ? "Blocked by incomplete dependencies."
                : $"Blocked by incomplete dependencies: {string.Join(", ", executionCheck.BlockedBy)}";
            return $"Task \"{task.Name}\" (ID: `{taskId}`) cannot be executed. {blockedBy}";
        }

        var updateResult = await workflowService.UpdateStatusAsync(taskId, TaskStatus.InProgress).ConfigureAwait(false);
        if (updateResult.IsFailure)
        {
            return $"Failed to update task status: {updateResult.Error}";
        }
        
        var complexity = workflowService.AssessComplexity(task);
        var dependenciesResult = await workflowService.LoadDependencyTasksAsync(task).ConfigureAwait(false);
        var dependencies = dependenciesResult.IsSuccess ? dependenciesResult.Value : ImmutableArray<TaskItem>.Empty;
        var relatedFilesSummary = workflowService.BuildRelatedFilesSummary(task);

        return promptBuilder.Build(task, complexity, relatedFilesSummary, dependencies);
    }

    [McpServerTool(Name = "verify_task")]
    [Description("Verify task completion with a score and summary.")]
    public static async Task<string> VerifyTask(
        VerifyTaskPromptBuilder promptBuilder,
        TaskWorkflowService workflowService,
        TaskStore taskStore,
        [Description("Task ID to verify.")] string taskId,
        [Description("Overall score from 0-100.")] int score,
        [Description("Summary of verification findings.")] string summary)
    {
        var taskMaybe = await taskStore.GetByIdAsync(taskId).ConfigureAwait(false);
        if (taskMaybe.HasNoValue)
        {
            return $"Task with ID `{taskId}` could not be found. Use list_tasks to confirm a valid ID.";
        }

        var task = taskMaybe.Value;
        if (task.Status != TaskStatus.InProgress)
        {
            var status = TaskStatusFormatter.ToSerializedValue(task.Status);
            return $"Task \"{task.Name}\" (ID: `{task.Id}`) is in status \"{status}\", not in progress. Use execute_task first.";
        }

        if (score >= 80)
        {
            var summaryResult = await workflowService.UpdateSummaryAsync(taskId, summary).ConfigureAwait(false);
            if (summaryResult.IsFailure)
            {
                return $"Failed to update summary: {summaryResult.Error}";
            }
            
            var statusResult = await workflowService.UpdateStatusAsync(taskId, TaskStatus.Completed).ConfigureAwait(false);
            if (statusResult.IsFailure)
            {
                return $"Failed to update status: {statusResult.Error}";
            }
        }

        return promptBuilder.Build(task, score, summary);
    }

    [McpServerTool(Name = "delete_task")]
    [Description("Delete an incomplete task by ID.")]
    public static async Task<string> DeleteTask(
        DeleteTaskPromptBuilder promptBuilder,
        TaskStore taskStore,
        [Description("Task ID to delete.")] string taskId)
    {
        var taskMaybe = await taskStore.GetByIdAsync(taskId).ConfigureAwait(false);
        if (taskMaybe.HasNoValue)
        {
            return promptBuilder.BuildNotFound(taskId);
        }

        var task = taskMaybe.Value;
        if (task.Status == TaskStatus.Completed)
        {
            return promptBuilder.BuildCompleted(task);
        }

        var deleteResult = await taskStore.DeleteAsync(taskId).ConfigureAwait(false);
        var message = deleteResult.IsSuccess
            ? $"Task \"{task.Name}\" (ID: `{task.Id}`) has been deleted."
            : $"Task \"{task.Name}\" (ID: `{task.Id}`) could not be deleted.";
        return promptBuilder.BuildResult(deleteResult.IsSuccess, message);
    }

    [McpServerTool(Name = "clear_all_tasks")]
    [Description("Clear all incomplete tasks after confirmation.")]
    public static async Task<string> ClearAllTasks(
        ClearAllTasksPromptBuilder promptBuilder,
        TaskStore taskStore,
        [Description("Confirmation flag to clear all tasks.")] bool confirm = false)
    {
        if (!confirm)
        {
            return promptBuilder.BuildCancel();
        }

        var tasksResult = await taskStore.GetAllAsync().ConfigureAwait(false);
        if (tasksResult.IsFailure)
        {
            return promptBuilder.BuildResult(false, tasksResult.Error, null);
        }

        var tasks = tasksResult.Value;
        if (tasks.IsDefaultOrEmpty)
        {
            return promptBuilder.BuildEmpty();
        }

        var clearResult = await taskStore.ClearAllAsync().ConfigureAwait(false);
        if (clearResult.IsFailure)
        {
            return promptBuilder.BuildResult(false, clearResult.Error, null);
        }

        var result = clearResult.Value;
        return promptBuilder.BuildResult(result.Success, result.Message, result.BackupFile);
    }

    [McpServerTool(Name = "update_task")]
    [Description("Update task details, dependencies, or related files.")]
    public static async Task<string> UpdateTask(
        UpdateTaskPromptBuilder promptBuilder,
        TaskStore taskStore,
        [Description("Task ID to update.")] string taskId,
        [Description("Updated task name.")] string? name = null,
        [Description("Updated task description.")] string? description = null,
        [Description("Updated task notes.")] string? notes = null,
        [Description("Updated dependencies (task IDs or names).")] string[]? dependencies = null,
        [Description("Updated related files.")] RelatedFileInput[]? relatedFiles = null,
        [Description("Updated implementation guide.")] string? implementationGuide = null,
        [Description("Updated verification criteria.")] string? verificationCriteria = null)
    {
        var validationError = RelatedFileValidator.ValidateLineNumbers(relatedFiles);
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            return promptBuilder.BuildValidation(validationError);
        }

        if (!HasUpdates(name, description, notes, dependencies, relatedFiles, implementationGuide, verificationCriteria))
        {
            return promptBuilder.BuildEmptyUpdate();
        }

        var taskMaybe = await taskStore.GetByIdAsync(taskId).ConfigureAwait(false);
        if (taskMaybe.HasNoValue)
        {
            return promptBuilder.BuildNotFound(taskId);
        }

        var resolvedDependenciesResult = await ResolveDependenciesAsync(taskStore, dependencies).ConfigureAwait(false);
        if (resolvedDependenciesResult.IsFailure)
        {
            return promptBuilder.BuildValidation(resolvedDependenciesResult.Error.ToString());
        }

        var request = new TaskUpdateRequest
        {
            Name = name,
            Description = description,
            Notes = notes,
            Dependencies = dependencies is null ? null : resolvedDependenciesResult.Value,
            RelatedFiles = relatedFiles is null
                ? null
                : RelatedFileConverter.ToRelatedFiles(relatedFiles),
            ImplementationGuide = implementationGuide,
            VerificationCriteria = verificationCriteria
        };

        var updateResult = await taskStore.UpdateAsync(taskId, request).ConfigureAwait(false);
        var success = updateResult.IsSuccess;
        var message = success ? "Task updated successfully." : "Task update failed.";
        return promptBuilder.BuildResult(success, message, updateResult.IsSuccess ? updateResult.Value : null);
    }

    private static bool HasUpdates(
        string? name,
        string? description,
        string? notes,
        string[]? dependencies,
        RelatedFileInput[]? relatedFiles,
        string? implementationGuide,
        string? verificationCriteria)
    {
        return !(string.IsNullOrWhiteSpace(name) &&
                 string.IsNullOrWhiteSpace(description) &&
                 string.IsNullOrWhiteSpace(notes) &&
                 dependencies is null &&
                 relatedFiles is null &&
                 string.IsNullOrWhiteSpace(implementationGuide) &&
                 string.IsNullOrWhiteSpace(verificationCriteria));
    }

    private static async Task<Result<ImmutableArray<string>>> ResolveDependenciesAsync(
        TaskStore taskStore,
        string[]? dependencies)
    {
        if (dependencies is null)
        {
            return Result.Success(ImmutableArray<string>.Empty);
        }

        var allTasksResult = await taskStore.GetAllAsync().ConfigureAwait(false);
        if (allTasksResult.IsFailure)
        {
            return Result.Failure<ImmutableArray<string>>(allTasksResult.Error);
        }

        var allTasks = allTasksResult.Value;
        var nameMap = TaskNameMap.Build(allTasks);
        var idSet = allTasks.Select(task => task.Id).ToImmutableHashSet();

        return TaskDependencyResolver.Resolve(dependencies, nameMap, idSet);
    }

    private static async Task<PlanTaskSnapshot> LoadPlanSnapshotAsync(
        TaskStore taskStore,
        bool existingTasksReference)
    {
        if (!existingTasksReference)
        {
            return PlanTaskSnapshot.Empty;
        }

        var tasksResult = await taskStore.GetAllAsync().ConfigureAwait(false);
        if (tasksResult.IsFailure)
        {
            // If we can't read tasks, return empty snapshot rather than failing the entire operation
            return PlanTaskSnapshot.Empty;
        }

        var tasks = tasksResult.Value;
        var completed = tasks.Where(task => task.Status == TaskStatus.Completed).ToImmutableArray();
        var pending = tasks.Where(task => task.Status != TaskStatus.Completed).ToImmutableArray();
        return new PlanTaskSnapshot(completed, pending);
    }

    private sealed record PlanTaskSnapshot(
        ImmutableArray<TaskItem> Completed,
        ImmutableArray<TaskItem> Pending)
    {
        public static PlanTaskSnapshot Empty { get; } =
            new(ImmutableArray<TaskItem>.Empty, ImmutableArray<TaskItem>.Empty);
    }
}
