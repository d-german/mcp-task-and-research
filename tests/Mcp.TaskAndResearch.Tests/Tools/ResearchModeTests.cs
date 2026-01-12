using Mcp.TaskAndResearch.Config;
using Mcp.TaskAndResearch.Data;
using Mcp.TaskAndResearch.Prompts;
using Mcp.TaskAndResearch.Tests.TestSupport;
using Mcp.TaskAndResearch.Tools.Research;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mcp.TaskAndResearch.Tests.Tools;

public sealed class ResearchModeTests
{
    [Fact]
    public void BuildPrompt_IncludesProvidedFieldsAndMemoryDir()
    {
        using var temp = new TempDirectory();
        using var env = CreateEnvScope(temp.Path);

        var resolver = new PathResolver(new WorkspaceRootStore(), new ConfigReader(), NullLogger<PathResolver>.Instance);
        var pathProvider = new DataPathProvider(resolver);
        var loader = new PromptTemplateLoader(resolver);
        var builder = new ResearchPromptBuilder(loader, pathProvider);

        var prompt = builder.BuildPrompt(new ResearchModeRequest
        {
            Topic = "Testing research tool",
            PreviousState = "Previous findings",
            CurrentState = "Review requirements",
            NextSteps = "Decide next actions"
        });

        Assert.Contains("Testing research tool", prompt);
        Assert.Contains("Previous findings", prompt);
        Assert.Contains("Review requirements", prompt);
        Assert.Contains("Decide next actions", prompt);
        Assert.Contains(pathProvider.GetPaths().MemoryDirectory, prompt);
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
}
