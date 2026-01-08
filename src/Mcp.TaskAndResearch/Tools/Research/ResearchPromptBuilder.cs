using System.Globalization;
using Mcp.TaskAndResearch.Data;
using Mcp.TaskAndResearch.Prompts;

namespace Mcp.TaskAndResearch.Tools.Research;

internal sealed class ResearchPromptBuilder
{
    private const string NoPreviousState =
        "This is the first research on this topic, no previous research state.";

    private readonly PromptTemplateLoader _templateLoader;
    private readonly DataPathProvider _pathProvider;

    public ResearchPromptBuilder(PromptTemplateLoader templateLoader, DataPathProvider pathProvider)
    {
        _templateLoader = templateLoader;
        _pathProvider = pathProvider;
    }

    public string BuildPrompt(ResearchModeRequest request)
    {
        var previousStateContent = BuildPreviousStateContent(request.PreviousState);
        var template = _templateLoader.LoadTemplate("researchMode/index.md");
        var memoryDir = _pathProvider.GetPaths().MemoryDirectory;
        var prompt = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["topic"] = request.Topic,
            ["previousStateContent"] = previousStateContent,
            ["currentState"] = request.CurrentState,
            ["nextSteps"] = request.NextSteps,
            ["memoryDir"] = memoryDir,
            ["time"] = DateTimeOffset.Now.ToString("f", CultureInfo.InvariantCulture)
        });

        return PromptCustomization.Apply(prompt, "RESEARCH_MODE");
    }

    private string BuildPreviousStateContent(string previousState)
    {
        if (string.IsNullOrWhiteSpace(previousState))
        {
            return NoPreviousState;
        }

        var template = _templateLoader.LoadTemplate("researchMode/previousState.md");
        return PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["previousState"] = previousState
        });
    }
}
