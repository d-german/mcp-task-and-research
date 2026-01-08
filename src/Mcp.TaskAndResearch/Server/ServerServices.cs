using Microsoft.Extensions.DependencyInjection;
using Mcp.TaskAndResearch.Config;
using Mcp.TaskAndResearch.Data;
using Mcp.TaskAndResearch.Prompts;
using Mcp.TaskAndResearch.Tools.Project;
using Mcp.TaskAndResearch.Tools.Research;
using Mcp.TaskAndResearch.Tools.Task;
using Mcp.TaskAndResearch.Tools.Thought;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Mcp.TaskAndResearch.Server;

internal static class ServerServices
{
    public static void Configure(IServiceCollection services)
    {
        var metadata = ServerMetadata.Default;

        services.AddSingleton<WorkspaceRootStore>();
        services.AddSingleton<ConfigReader>();
        services.AddSingleton<PathResolver>();
        services.AddSingleton<DataPathProvider>();
        services.AddSingleton<MemoryStore>();
        services.AddSingleton<TaskStore>();
        services.AddSingleton<RulesStore>();
        services.AddSingleton<TaskSearchService>();
        services.AddSingleton<PromptTemplateLoader>();
        services.AddSingleton<ResearchPromptBuilder>();
        services.AddSingleton<AnalyzeTaskPromptBuilder>();
        services.AddSingleton<ReflectTaskPromptBuilder>();
        services.AddSingleton<PlanTaskPromptBuilder>();
        services.AddSingleton<SplitTasksPromptBuilder>();
        services.AddSingleton<ExecuteTaskPromptBuilder>();
        services.AddSingleton<VerifyTaskPromptBuilder>();
        services.AddSingleton<ListTasksPromptBuilder>();
        services.AddSingleton<QueryTaskPromptBuilder>();
        services.AddSingleton<GetTaskDetailPromptBuilder>();
        services.AddSingleton<UpdateTaskPromptBuilder>();
        services.AddSingleton<DeleteTaskPromptBuilder>();
        services.AddSingleton<ClearAllTasksPromptBuilder>();
        services.AddSingleton<ProcessThoughtPromptBuilder>();
        services.AddSingleton<InitProjectRulesPromptBuilder>();
        services.AddSingleton<TaskUpdatePlanner>();
        services.AddSingleton<TaskBatchService>();
        services.AddSingleton<TaskComplexityAssessor>();
        services.AddSingleton<RelatedFilesSummaryBuilder>();
        services.AddSingleton<TaskWorkflowService>();
        services.AddSingleton(metadata);
        services.AddSingleton<McpServerAccessor>();

        services.AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = metadata.Name,
                    Version = metadata.Version
                };
            })
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
    }
}
