using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Mcp.TaskAndResearch.Tools.Research;

[McpServerToolType]
internal static class ResearchModeTool
{
    [McpServerTool(Name = "research_mode")]
    [Description("Enter a research-focused mode to explore a programming topic in depth.")]
    public static string ResearchMode(
        ResearchPromptBuilder promptBuilder,
        [Description("Programming topic content to be researched, should be clear and specific.")] string topic,
        [Description("Previous research state and content summary, empty on first execution.")] string? previousState,
        [Description("Main content that the current agent should execute.")] string currentState,
        [Description("Subsequent plans, steps, or research directions.")] string nextSteps)
    {
        var request = new ResearchModeRequest
        {
            Topic = topic,
            PreviousState = previousState ?? string.Empty,
            CurrentState = currentState,
            NextSteps = nextSteps
        };

        return promptBuilder.BuildPrompt(request);
    }
}
