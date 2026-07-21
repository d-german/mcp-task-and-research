using System.ComponentModel;
using Mcp.ProcessThought.Prompts;
using ModelContextProtocol.Server;

namespace Mcp.ProcessThought.Tools;

[McpServerToolType]
internal static class ThoughtTools
{
    [McpServerTool(Name = "process_thought")]
    [Description("Guide iterative, evidence-based reasoning; continue only when useful.")]
    public static string ProcessThought(
        ProcessThoughtPromptBuilder promptBuilder,
        [Description("Evidence and conclusions for this step.")] string thought,
        [Description("1-based step.")] int thought_number,
        [Description("Expected steps; increase if needed.")] int total_thoughts,
        [Description("Phase, e.g. Analysis or Decision.")] string stage,
        [Description("Optional topic labels.")] string[]? tags = null,
        [Description("Optional principles used.")] string[]? axioms_used = null,
        [Description("Optional challenged assumptions.")] string[]? assumptions_challenged = null,
        [Description("True only if another step is needed.")] bool next_thought_needed = false)
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
