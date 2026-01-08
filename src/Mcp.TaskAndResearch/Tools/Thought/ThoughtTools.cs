using System.ComponentModel;
using Mcp.TaskAndResearch.Prompts;
using ModelContextProtocol.Server;

namespace Mcp.TaskAndResearch.Tools.Thought;

internal sealed class ProcessThoughtPromptBuilder
{
    private readonly PromptTemplateLoader _templateLoader;

    public ProcessThoughtPromptBuilder(PromptTemplateLoader templateLoader)
    {
        _templateLoader = templateLoader;
    }

    public string Build(
        string thought,
        int thoughtNumber,
        int totalThoughts,
        string stage,
        string[] tags,
        string[] axiomsUsed,
        string[] assumptionsChallenged,
        bool nextThoughtNeeded)
    {
        var nextThoughtTemplate = nextThoughtNeeded
            ? _templateLoader.LoadTemplate("processThought/moreThought.md")
            : _templateLoader.LoadTemplate("processThought/complatedThought.md");
        var indexTemplate = _templateLoader.LoadTemplate("processThought/index.md");
        var prompt = PromptTemplateRenderer.Render(indexTemplate, new Dictionary<string, object?>
        {
            ["thought"] = thought,
            ["thoughtNumber"] = thoughtNumber,
            ["totalThoughts"] = totalThoughts,
            ["stage"] = stage,
            ["tags"] = FormatList(tags, "no tags"),
            ["axioms_used"] = FormatList(axiomsUsed, "no axioms used"),
            ["assumptions_challenged"] = FormatList(assumptionsChallenged, "no assumptions challenged"),
            ["nextThoughtNeeded"] = nextThoughtTemplate
        });

        return PromptCustomization.Apply(prompt, "PROCESS_THOUGHT");
    }

    private static string FormatList(string[] values, string fallback)
    {
        if (values.Length == 0)
        {
            return fallback;
        }

        return string.Join(", ", values);
    }
}

[McpServerToolType]
internal static class ThoughtTools
{
    [McpServerTool(Name = "process_thought")]
    [Description("Record a structured thought step in a reasoning process.")]
    public static string ProcessThought(
        ProcessThoughtPromptBuilder promptBuilder,
        [Description("Thought content.")] string thought,
        [Description("Current thought number.")] int thought_number,
        [Description("Total number of thoughts.")] int total_thoughts,
        [Description("Thinking stage (e.g., Analysis, Planning).")] string stage,
        [Description("Tags describing the thought.")] string[]? tags = null,
        [Description("Axioms or principles used.")] string[]? axioms_used = null,
        [Description("Assumptions challenged.")] string[]? assumptions_challenged = null,
        [Description("Whether further thought is needed.")] bool next_thought_needed = false)
    {
        return promptBuilder.Build(
            thought,
            thought_number,
            total_thoughts,
            stage,
            tags ?? Array.Empty<string>(),
            axioms_used ?? Array.Empty<string>(),
            assumptions_challenged ?? Array.Empty<string>(),
            next_thought_needed);
    }
}
