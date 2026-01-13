using System.Collections.Immutable;
using System.Globalization;
using Mcp.TaskAndResearch.Data;
using Mcp.TaskAndResearch.Prompts;
using TaskStatus = Mcp.TaskAndResearch.Data.TaskStatus;

namespace Mcp.TaskAndResearch.Tools.Task;

internal sealed class AnalyzeTaskPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public AnalyzeTaskPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(string summary, string initialConcept, string? previousAnalysis)
    {
        var iterationPrompt = BuildIteration(previousAnalysis);
        var template = _templateLoader.LoadTemplateOrThrow("analyzeTask/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["summary"] = summary,
            ["initialConcept"] = initialConcept,
            ["iterationPrompt"] = iterationPrompt
        });

        return PromptCustomization.Apply(prompt, "ANALYZE_TASK");
    }

    private string BuildIteration(string? previousAnalysis)
    {
        if (string.IsNullOrWhiteSpace(previousAnalysis))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("analyzeTask/iteration.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["previousAnalysis"] = previousAnalysis
        });
    }
}

internal sealed class ReflectTaskPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public ReflectTaskPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(string summary, string analysis)
    {
        var template = _templateLoader.LoadTemplateOrThrow("reflectTask/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["summary"] = summary,
            ["analysis"] = analysis
        });

        return PromptCustomization.Apply(prompt, "REFLECT_TASK");
    }
}

internal sealed class PlanTaskPromptBuilder
{
    private const int CompletedTaskLimit = 10;
    private readonly PromptTemplateLoader _templateLoader;

    public PlanTaskPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(
        string description,
        string? requirements,
        bool existingTasksReference,
        ImmutableArray<TaskItem> completedTasks,
        ImmutableArray<TaskItem> pendingTasks,
        string memoryDir,
        string rulesPath)
    {
        var tasksTemplate = BuildTasksTemplate(existingTasksReference, completedTasks, pendingTasks);
        var thoughtTemplate = LoadThoughtTemplate();
        var template = _templateLoader.LoadTemplateOrThrow("planTask/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["description"] = description,
            ["requirements"] = string.IsNullOrWhiteSpace(requirements) ? "No requirements" : requirements,
            ["tasksTemplate"] = tasksTemplate,
            ["rulesPath"] = rulesPath,
            ["memoryDir"] = memoryDir,
            ["thoughtTemplate"] = thoughtTemplate
        });

        return PromptCustomization.Apply(prompt, "PLAN_TASK");
    }

    private string BuildTasksTemplate(
        bool existingTasksReference,
        ImmutableArray<TaskItem> completedTasks,
        ImmutableArray<TaskItem> pendingTasks)
    {
        if (!existingTasksReference)
        {
            return string.Empty;
        }

        var totalCount = completedTasks.Length + pendingTasks.Length;
        if (totalCount == 0)
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("planTask/tasks.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["completedTasks"] = BuildCompletedTasksContent(completedTasks),
            ["unfinishedTasks"] = BuildPendingTasksContent(pendingTasks)
        });
    }

    private static string BuildCompletedTasksContent(ImmutableArray<TaskItem> completedTasks)
    {
        if (completedTasks.IsDefaultOrEmpty)
        {
            return "no completed tasks";
        }

        var tasksToShow = completedTasks.Length > CompletedTaskLimit
            ? completedTasks.Take(CompletedTaskLimit).ToImmutableArray()
            : completedTasks;

        var builder = new List<string>(tasksToShow.Length);
        for (var index = 0; index < tasksToShow.Length; index++)
        {
            var task = tasksToShow[index];
            builder.Add(FormatCompletedTask(index + 1, task));
        }

        var content = string.Join("\n\n", builder);
        if (completedTasks.Length > CompletedTaskLimit)
        {
            content += $"\n\n*(showing first {CompletedTaskLimit} of {completedTasks.Length})*\n";
        }

        return content;
    }

    private static string BuildPendingTasksContent(ImmutableArray<TaskItem> pendingTasks)
    {
        if (pendingTasks.IsDefaultOrEmpty)
        {
            return "no pending tasks";
        }

        var builder = new List<string>(pendingTasks.Length);
        for (var index = 0; index < pendingTasks.Length; index++)
        {
            var task = pendingTasks[index];
            builder.Add(FormatPendingTask(index + 1, task));
        }

        return string.Join("\n\n", builder);
    }

    private static string FormatCompletedTask(int index, TaskItem task)
    {
        var description = Truncate(task.Description, 100);
        var completedAt = task.CompletedAt.HasValue
            ? $"   - completedAt: {TaskDateFormatter.Format(task.CompletedAt.Value)}\n"
            : string.Empty;

        return $"{index}. **{task.Name}** (ID: `{task.Id}`)\n   - description: {description}\n{completedAt}";
    }

    private static string FormatPendingTask(int index, TaskItem task)
    {
        var description = Truncate(task.Description, 150);
        var dependencies = FormatDependencies(task.Dependencies);

        return $"{index}. **{task.Name}** (ID: `{task.Id}`)\n" +
               $"   - description: {description}\n" +
               $"   - status: {TaskStatusFormatter.ToSerializedValue(task.Status)}\n" +
               $"{dependencies}";
    }

    private static string FormatDependencies(ImmutableArray<TaskDependency> dependencies)
    {
        if (dependencies.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var list = string.Join(", ", dependencies.Select(dep => $"`{dep.TaskId}`"));
        return $"   - dependencies: {list}\n";
    }

    private string LoadThoughtTemplate()
    {
        var thoughtEnabled = Environment.GetEnvironmentVariable("ENABLE_THOUGHT_CHAIN");
        var templatePath = thoughtEnabled?.Equals("false", StringComparison.OrdinalIgnoreCase) == true
            ? "planTask/noThought.md"
            : "planTask/hasThought.md";

        return _templateLoader.LoadTemplateOrThrow(templatePath);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...";
    }
}

internal sealed class SplitTasksPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public SplitTasksPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(
        string updateMode,
        ImmutableArray<TaskItem> createdTasks,
        ImmutableArray<TaskItem> allTasks)
    {
        var taskDetailsTemplate = _templateLoader.LoadTemplateOrThrow("splitTasks/taskDetails.md");
        var tasksContent = BuildTasksContent(taskDetailsTemplate, createdTasks, allTasks);
        var indexTemplate = _templateLoader.LoadTemplateOrThrow("splitTasks/index.md");
        var prompt = PromptTemplateRenderer.Render(indexTemplate, new Dictionary<string, object?>
        {
            ["updateMode"] = updateMode,
            ["tasksContent"] = tasksContent
        });

        return PromptCustomization.Apply(prompt, "SPLIT_TASKS");
    }

    private static string BuildTasksContent(
        string taskDetailsTemplate,
        ImmutableArray<TaskItem> createdTasks,
        ImmutableArray<TaskItem> allTasks)
    {
        if (createdTasks.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var builder = new List<string>(createdTasks.Length);
        for (var index = 0; index < createdTasks.Length; index++)
        {
            var task = createdTasks[index];
            var content = PromptTemplateRenderer.Render(taskDetailsTemplate, new Dictionary<string, object?>
            {
                ["index"] = index + 1,
                ["name"] = task.Name,
                ["id"] = task.Id,
                ["description"] = task.Description,
                ["notes"] = string.IsNullOrWhiteSpace(task.Notes) ? "no notes" : task.Notes,
                ["implementationGuide"] = Truncate(task.ImplementationGuide, "no implementation guide"),
                ["verificationCriteria"] = Truncate(task.VerificationCriteria, "no verification criteria"),
                ["dependencies"] = FormatDependencies(task.Dependencies, allTasks)
            });

            builder.Add(content);
        }

        return string.Join("\n", builder);
    }

    private static string FormatDependencies(
        ImmutableArray<TaskDependency> dependencies,
        ImmutableArray<TaskItem> allTasks)
    {
        if (dependencies.IsDefaultOrEmpty)
        {
            return "no dependencies";
        }

        var items = new List<string>(dependencies.Length);
        foreach (var dependency in dependencies)
        {
            var name = allTasks.FirstOrDefault(task => task.Id == dependency.TaskId)?.Name;
            items.Add(string.IsNullOrWhiteSpace(name)
                ? $"`{dependency.TaskId}`"
                : $"\"{name}\" (`{dependency.TaskId}`)");
        }

        return string.Join(", ", items);
    }

    private static string Truncate(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Length > 100 ? value.Substring(0, 100) + "..." : value;
    }
}

internal sealed class ExecuteTaskPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public ExecuteTaskPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(
        TaskItem task,
        TaskComplexityAssessment? complexityAssessment,
        string relatedFilesSummary,
        ImmutableArray<TaskItem> dependencyTasks)
    {
        var notesPrompt = BuildNotesPrompt(task);
        var implementationGuidePrompt = BuildImplementationGuidePrompt(task);
        var verificationCriteriaPrompt = BuildVerificationCriteriaPrompt(task);
        var analysisResultPrompt = BuildAnalysisResultPrompt(task);
        var dependencyTasksPrompt = BuildDependencyTasksPrompt(dependencyTasks);
        var relatedFilesSummaryPrompt = BuildRelatedFilesSummaryPrompt(relatedFilesSummary);
        var complexityPrompt = BuildComplexityPrompt(complexityAssessment);

        var template = _templateLoader.LoadTemplateOrThrow("executeTask/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["name"] = task.Name,
            ["id"] = task.Id,
            ["description"] = task.Description,
            ["notesTemplate"] = notesPrompt,
            ["implementationGuideTemplate"] = implementationGuidePrompt,
            ["verificationCriteriaTemplate"] = verificationCriteriaPrompt,
            ["analysisResultTemplate"] = analysisResultPrompt,
            ["dependencyTasksTemplate"] = dependencyTasksPrompt,
            ["relatedFilesSummaryTemplate"] = relatedFilesSummaryPrompt,
            ["complexityTemplate"] = complexityPrompt
        });

        if (!string.IsNullOrWhiteSpace(task.Agent))
        {
            prompt = $"use sub-agent {task.Agent}\n\n{prompt}";
        }

        return PromptCustomization.Apply(prompt, "EXECUTE_TASK");
    }

    private string BuildNotesPrompt(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.Notes))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("executeTask/notes.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["notes"] = task.Notes
        });
    }

    private string BuildImplementationGuidePrompt(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.ImplementationGuide))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("executeTask/implementationGuide.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["implementationGuide"] = task.ImplementationGuide
        });
    }

    private string BuildVerificationCriteriaPrompt(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.VerificationCriteria))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("executeTask/verificationCriteria.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["verificationCriteria"] = task.VerificationCriteria
        });
    }

    private string BuildAnalysisResultPrompt(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.AnalysisResult))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("executeTask/analysisResult.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["analysisResult"] = task.AnalysisResult
        });
    }

    private string BuildDependencyTasksPrompt(ImmutableArray<TaskItem> dependencyTasks)
    {
        if (dependencyTasks.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var completed = dependencyTasks
            .Where(task => task.Status == TaskStatus.Completed && !string.IsNullOrWhiteSpace(task.Summary))
            .ToImmutableArray();
        if (completed.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var content = string.Join("\n\n", completed.Select(FormatDependencySummary));
        var template = _templateLoader.LoadTemplateOrThrow("executeTask/dependencyTasks.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["dependencyTasks"] = content + "\n"
        });
    }

    private string BuildRelatedFilesSummaryPrompt(string relatedFilesSummary)
    {
        var template = _templateLoader.LoadTemplateOrThrow("executeTask/relatedFilesSummary.md");
        var summary = string.IsNullOrWhiteSpace(relatedFilesSummary)
            ? "The current task has no associated files."
            : relatedFilesSummary;

        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["relatedFilesSummary"] = summary
        });
    }

    private string BuildComplexityPrompt(TaskComplexityAssessment? assessment)
    {
        if (assessment is null)
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("executeTask/complexity.md");
        var recommendations = BuildRecommendationContent(assessment.Recommendations);
        var levelLabel = TaskComplexityLevelFormatter.ToLabel(assessment.Level);
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["level"] = levelLabel,
            ["complexityStyle"] = GetComplexityStyle(assessment.Level),
            ["descriptionLength"] = assessment.Metrics.DescriptionLength,
            ["dependenciesCount"] = assessment.Metrics.DependenciesCount,
            ["recommendation"] = recommendations
        });
    }

    private static string FormatDependencySummary(TaskItem task)
    {
        var summary = string.IsNullOrWhiteSpace(task.Summary) ? "*No completion summary*" : task.Summary;
        return $"### {task.Name}\n{summary}";
    }

    private static string BuildRecommendationContent(ImmutableArray<string> recommendations)
    {
        if (recommendations.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        return string.Join("\n", recommendations.Select(rec => $"- {rec}")) + "\n";
    }

    private static string GetComplexityStyle(TaskComplexityLevel level)
    {
        return level switch
        {
            TaskComplexityLevel.VeryHigh => "WARNING: This task is extremely complex.",
            TaskComplexityLevel.High => "WARNING: This task has high complexity.",
            TaskComplexityLevel.Medium => "TIP: This task has moderate complexity.",
            _ => string.Empty
        };
    }
}

internal sealed class VerifyTaskPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public VerifyTaskPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(TaskItem task, int score, string summary)
    {
        if (score < 80)
        {
            var noPassTemplate = _templateLoader.LoadTemplateOrThrow("verifyTask/noPass.md");
            return PromptTemplateRenderer.Render(noPassTemplate, new Dictionary<string, object?>
            {
                ["name"] = task.Name,
                ["id"] = task.Id,
                ["summary"] = summary
            });
        }

        var template = _templateLoader.LoadTemplateOrThrow("verifyTask/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["name"] = task.Name,
            ["id"] = task.Id,
            ["summary"] = summary
        });

        return PromptCustomization.Apply(prompt, "VERIFY_TASK");
    }
}

internal sealed class ListTasksPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public ListTasksPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(string status, ImmutableArray<TaskItem> allTasks, ImmutableArray<TaskItem> filteredTasks)
    {
        if (filteredTasks.IsDefaultOrEmpty)
        {
            return BuildNotFound(status);
        }

        var tasksByStatus = BucketTasks(allTasks);
        var statusCounts = BuildStatusCounts(tasksByStatus);
        var taskDetails = BuildTaskDetails(tasksByStatus, TaskStatusFormatter.ParseFilter(status));
        var template = _templateLoader.LoadTemplateOrThrow("listTasks/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["statusCount"] = statusCounts,
            ["taskDetailsTemplate"] = taskDetails
        });

        return PromptCustomization.Apply(prompt, "LIST_TASKS");
    }

    private string BuildNotFound(string status)
    {
        var statusText = string.Equals(status, "all", StringComparison.OrdinalIgnoreCase)
            ? "any"
            : $"any {status}";
        var template = _templateLoader.LoadTemplateOrThrow("listTasks/notFound.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["statusText"] = statusText
        });
    }

    private static Dictionary<TaskStatus, ImmutableArray<TaskItem>> BucketTasks(ImmutableArray<TaskItem> tasks)
    {
        var map = new Dictionary<TaskStatus, ImmutableArray<TaskItem>>();
        foreach (var task in tasks)
        {
            if (!map.TryGetValue(task.Status, out var list))
            {
                list = ImmutableArray<TaskItem>.Empty;
            }

            map[task.Status] = list.Add(task);
        }

        return map;
    }

    private static string BuildStatusCounts(Dictionary<TaskStatus, ImmutableArray<TaskItem>> tasksByStatus)
    {
        var lines = new List<string>();
        foreach (var status in Enum.GetValues<TaskStatus>())
        {
            tasksByStatus.TryGetValue(status, out var tasks);
            var count = tasks.IsDefaultOrEmpty ? 0 : tasks.Length;
            lines.Add($"- **{TaskStatusFormatter.ToSerializedValue(status)}**: {count} tasks");
        }

        return string.Join("\n", lines);
    }

    private string BuildTaskDetails(
        Dictionary<TaskStatus, ImmutableArray<TaskItem>> tasksByStatus,
        TaskStatus? filterStatus)
    {
        var template = _templateLoader.LoadTemplateOrThrow("listTasks/taskDetails.md");
        var builder = new List<string>();
        foreach (var status in Enum.GetValues<TaskStatus>())
        {
            builder.AddRange(BuildStatusDetails(status, filterStatus, tasksByStatus, template));
        }

        return string.Join("\n", builder);
    }

    private IEnumerable<string> BuildStatusDetails(
        TaskStatus status,
        TaskStatus? filterStatus,
        Dictionary<TaskStatus, ImmutableArray<TaskItem>> tasksByStatus,
        string template)
    {
        if (filterStatus.HasValue && status != filterStatus.Value)
        {
            return Array.Empty<string>();
        }

        if (!tasksByStatus.TryGetValue(status, out var tasks) || tasks.IsDefaultOrEmpty)
        {
            return Array.Empty<string>();
        }

        return tasks.Select(task => RenderTaskDetails(template, task));
    }

    private static string RenderTaskDetails(string template, TaskItem task)
    {
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["name"] = task.Name,
            ["id"] = task.Id,
            ["description"] = task.Description,
            ["createAt"] = TaskDateFormatter.Format(task.CreatedAt),
            ["complatedSummary"] = Truncate(task.Summary, 100),
            ["dependencies"] = FormatDependencies(task.Dependencies),
            ["complatedAt"] = FormatCompletedAt(task)
        });
    }

    private static string FormatCompletedAt(TaskItem task)
    {
        return task.CompletedAt.HasValue
            ? TaskDateFormatter.Format(task.CompletedAt.Value)
            : string.Empty;
    }

    private static string FormatDependencies(ImmutableArray<TaskDependency> dependencies)
    {
        if (dependencies.IsDefaultOrEmpty)
        {
            return "none";
        }

        return string.Join(", ", dependencies.Select(dep => $"`{dep.TaskId}`"));
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length > maxLength ? value.Substring(0, maxLength) + "..." : value;
    }
}

internal sealed class QueryTaskPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public QueryTaskPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(
        string query,
        ImmutableArray<TaskItem> tasks,
        int totalTasks,
        int page,
        int pageSize,
        int totalPages)
    {
        if (tasks.IsDefaultOrEmpty)
        {
            var notFoundTemplate = _templateLoader.LoadTemplateOrThrow("queryTask/notFound.md");
            return PromptTemplateRenderer.Render(notFoundTemplate, new Dictionary<string, object?>
            {
                ["query"] = query
            });
        }

        var tasksContent = BuildTasksContent(tasks);
        var indexTemplate = _templateLoader.LoadTemplateOrThrow("queryTask/index.md");
        var prompt = PromptTemplateRenderer.Render(indexTemplate, new Dictionary<string, object?>
        {
            ["tasksContent"] = tasksContent,
            ["page"] = page,
            ["totalPages"] = totalPages,
            ["pageSize"] = pageSize,
            ["totalTasks"] = totalTasks,
            ["query"] = query
        });

        return PromptCustomization.Apply(prompt, "QUERY_TASK");
    }

    private string BuildTasksContent(ImmutableArray<TaskItem> tasks)
    {
        var template = _templateLoader.LoadTemplateOrThrow("queryTask/taskDetails.md");
        var builder = new List<string>(tasks.Length);
        foreach (var task in tasks)
        {
            builder.Add(PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
            {
                ["taskId"] = task.Id,
                ["taskName"] = task.Name,
                ["taskStatus"] = TaskStatusFormatter.ToSerializedValue(task.Status),
                ["taskDescription"] = Truncate(task.Description, 100),
                ["createdAt"] = TaskDateFormatter.Format(task.CreatedAt)
            }));
        }

        return string.Join("\n", builder);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...";
    }
}

internal sealed class GetTaskDetailPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public GetTaskDetailPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(string taskId, TaskItem? task, string? error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            var errorTemplate = _templateLoader.LoadTemplateOrThrow("getTaskDetail/error.md");
            return PromptTemplateRenderer.Render(errorTemplate, new Dictionary<string, object?>
            {
                ["errorMessage"] = error
            });
        }

        if (task is null)
        {
            var notFoundTemplate = _templateLoader.LoadTemplateOrThrow("getTaskDetail/notFound.md");
            return PromptTemplateRenderer.Render(notFoundTemplate, new Dictionary<string, object?>
            {
                ["taskId"] = taskId
            });
        }

        var notesPrompt = BuildNotesPrompt(task);
        var dependenciesPrompt = BuildDependenciesPrompt(task);
        var implementationGuidePrompt = BuildImplementationGuidePrompt(task);
        var verificationCriteriaPrompt = BuildVerificationCriteriaPrompt(task);
        var relatedFilesPrompt = BuildRelatedFilesPrompt(task);
        var completedSummaryPrompt = BuildCompletedSummaryPrompt(task);

        var template = _templateLoader.LoadTemplateOrThrow("getTaskDetail/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["name"] = task.Name,
            ["id"] = task.Id,
            ["status"] = TaskStatusFormatter.ToSerializedValue(task.Status),
            ["description"] = task.Description,
            ["notesTemplate"] = notesPrompt,
            ["dependenciesTemplate"] = dependenciesPrompt,
            ["implementationGuideTemplate"] = implementationGuidePrompt,
            ["verificationCriteriaTemplate"] = verificationCriteriaPrompt,
            ["relatedFilesTemplate"] = relatedFilesPrompt,
            ["createdTime"] = TaskDateFormatter.Format(task.CreatedAt),
            ["updatedTime"] = TaskDateFormatter.Format(task.UpdatedAt),
            ["complatedSummaryTemplate"] = completedSummaryPrompt
        });

        return PromptCustomization.Apply(prompt, "GET_TASK_DETAIL");
    }

    private string BuildNotesPrompt(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.Notes))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("getTaskDetail/notes.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["notes"] = task.Notes
        });
    }

    private string BuildDependenciesPrompt(TaskItem task)
    {
        if (task.Dependencies.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("getTaskDetail/dependencies.md");
        var dependencies = string.Join(", ", task.Dependencies.Select(dep => $"`{dep.TaskId}`"));
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["dependencies"] = dependencies
        });
    }

    private string BuildImplementationGuidePrompt(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.ImplementationGuide))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("getTaskDetail/implementationGuide.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["implementationGuide"] = task.ImplementationGuide
        });
    }

    private string BuildVerificationCriteriaPrompt(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.VerificationCriteria))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("getTaskDetail/verificationCriteria.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["verificationCriteria"] = task.VerificationCriteria
        });
    }

    private string BuildRelatedFilesPrompt(TaskItem task)
    {
        if (task.RelatedFiles.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var lines = task.RelatedFiles.Select(file =>
        {
            var description = string.IsNullOrWhiteSpace(file.Description) ? string.Empty : $": {file.Description}";
            return $"- `{file.Path}` ({file.Type}){description}";
        });

        var template = _templateLoader.LoadTemplateOrThrow("getTaskDetail/relatedFiles.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["files"] = string.Join("\n", lines)
        });
    }

    private string BuildCompletedSummaryPrompt(TaskItem task)
    {
        if (!task.CompletedAt.HasValue)
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("getTaskDetail/complatedSummary.md");
        var summary = string.IsNullOrWhiteSpace(task.Summary) ? "*No completion summary*" : task.Summary;
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["completedTime"] = TaskDateFormatter.Format(task.CompletedAt.Value),
            ["summary"] = summary
        });
    }
}

internal sealed class UpdateTaskPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public UpdateTaskPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string BuildNotFound(string taskId)
    {
        var template = _templateLoader.LoadTemplateOrThrow("updateTaskContent/notFound.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["taskId"] = taskId
        });
    }

    public string BuildValidation(string error)
    {
        var template = _templateLoader.LoadTemplateOrThrow("updateTaskContent/validation.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["error"] = error
        });
    }

    public string BuildEmptyUpdate()
    {
        var template = _templateLoader.LoadTemplateOrThrow("updateTaskContent/emptyUpdate.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>());
    }

    public string BuildResult(bool success, string message, TaskItem? updatedTask)
    {
        var responseTitle = success ? "Success" : "Failure";
        var content = message ?? string.Empty;
        if (success && updatedTask is not null)
        {
            content += BuildSuccessDetails(updatedTask);
        }

        var template = _templateLoader.LoadTemplateOrThrow("updateTaskContent/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["responseTitle"] = responseTitle,
            ["message"] = content
        });

        return PromptCustomization.Apply(prompt, "UPDATE_TASK_CONTENT");
    }

    private string BuildSuccessDetails(TaskItem updatedTask)
    {
        var template = _templateLoader.LoadTemplateOrThrow("updateTaskContent/success.md");
        var filesContent = BuildRelatedFilesContent(updatedTask.RelatedFiles);
        var taskNotes = BuildNotes(updatedTask.Notes);
        var description = Truncate(updatedTask.Description, 100);

        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["taskName"] = updatedTask.Name,
            ["taskDescription"] = description,
            ["taskNotes"] = taskNotes,
            ["taskStatus"] = TaskStatusFormatter.ToSerializedValue(updatedTask.Status),
            ["taskUpdatedAt"] = updatedTask.UpdatedAt.ToString("O", CultureInfo.InvariantCulture),
            ["filesContent"] = filesContent
        });
    }

    private string BuildRelatedFilesContent(ImmutableArray<RelatedFile> relatedFiles)
    {
        if (relatedFiles.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("updateTaskContent/fileDetails.md");
        var grouped = relatedFiles.GroupBy(file => file.Type);
        var content = string.Empty;
        foreach (var group in grouped)
        {
            var filesList = string.Join(", ", group.Select(file => $"`{file.Path}`"));
            content += PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
            {
                ["fileType"] = group.Key,
                ["fileCount"] = group.Count(),
                ["filesList"] = filesList
            });
        }

        return content;
    }

    private static string BuildNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return string.Empty;
        }

        var truncated = notes.Length > 100 ? notes.Substring(0, 100) + "..." : notes;
        return $"- **Notes:** {truncated}\n";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...";
    }
}

internal sealed class DeleteTaskPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public DeleteTaskPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string BuildNotFound(string taskId)
    {
        var template = _templateLoader.LoadTemplateOrThrow("deleteTask/notFound.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["taskId"] = taskId
        });
    }

    public string BuildCompleted(TaskItem task)
    {
        var template = _templateLoader.LoadTemplateOrThrow("deleteTask/completed.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["taskName"] = task.Name,
            ["taskId"] = task.Id
        });
    }

    public string BuildResult(bool success, string message)
    {
        var responseTitle = success ? "Success" : "Failure";
        var template = _templateLoader.LoadTemplateOrThrow("deleteTask/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["responseTitle"] = responseTitle,
            ["message"] = message
        });

        return PromptCustomization.Apply(prompt, "DELETE_TASK");
    }
}

internal sealed class ClearAllTasksPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public ClearAllTasksPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string BuildCancel()
    {
        var template = _templateLoader.LoadTemplateOrThrow("clearAllTasks/cancel.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>());
    }

    public string BuildEmpty()
    {
        var template = _templateLoader.LoadTemplateOrThrow("clearAllTasks/empty.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>());
    }

    public string BuildResult(bool success, string message, string? backupFile)
    {
        var responseTitle = success ? "Success" : "Failure";
        var backupInfo = BuildBackupInfo(backupFile);
        var template = _templateLoader.LoadTemplateOrThrow("clearAllTasks/index.md");
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["responseTitle"] = responseTitle,
            ["message"] = message ?? string.Empty,
            ["backupInfo"] = backupInfo
        });

        return PromptCustomization.Apply(prompt, "CLEAR_ALL_TASKS");
    }

    private string BuildBackupInfo(string? backupFile)
    {
        if (string.IsNullOrWhiteSpace(backupFile))
        {
            return string.Empty;
        }

        var template = _templateLoader.LoadTemplateOrThrow("clearAllTasks/backupInfo.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["backupFile"] = backupFile
        });
    }
}
