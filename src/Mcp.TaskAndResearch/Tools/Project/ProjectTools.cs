using System.ComponentModel;
using Mcp.TaskAndResearch.Prompts;
using ModelContextProtocol.Server;

namespace Mcp.TaskAndResearch.Tools.Project;

internal sealed class InitProjectRulesPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public InitProjectRulesPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build()
    {
        var template = _templateLoader.LoadTemplateOrThrow("initProjectRules/index.md");
        return PromptCustomization.Apply(template, "INIT_PROJECT_RULES");
    }
}

[McpServerToolType]
internal static class ProjectTools
{
    [McpServerTool(Name = "init_project_rules")]
    [Description("Initialize or update the project rules document.")]
    public static string InitProjectRules(InitProjectRulesPromptBuilder promptBuilder)
    {
        return promptBuilder.Build();
    }
}
