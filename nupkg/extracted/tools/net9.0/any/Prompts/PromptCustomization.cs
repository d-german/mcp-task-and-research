namespace Mcp.TaskAndResearch.Prompts;

internal static class PromptCustomization
{
    public static string Apply(string basePrompt, string promptKey)
    {
        var envKey = promptKey.ToUpperInvariant();
        var overrideValue = Environment.GetEnvironmentVariable($"MCP_PROMPT_{envKey}");

        if (!string.IsNullOrWhiteSpace(overrideValue))
        {
            return DecodeEnvironmentString(overrideValue);
        }

        var appendValue = Environment.GetEnvironmentVariable($"MCP_PROMPT_{envKey}_APPEND");
        if (!string.IsNullOrWhiteSpace(appendValue))
        {
            return $"{basePrompt}{Environment.NewLine}{Environment.NewLine}{DecodeEnvironmentString(appendValue)}";
        }

        return basePrompt;
    }

    private static string DecodeEnvironmentString(string value)
    {
        return value
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\t", "\t", StringComparison.Ordinal)
            .Replace("\\r", "\r", StringComparison.Ordinal);
    }
}
