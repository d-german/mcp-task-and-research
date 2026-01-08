namespace Mcp.TaskAndResearch.Prompts;

internal static class PromptTemplateRenderer
{
    public static string Render(string template, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return template;
        }

        var result = template;
        foreach (var entry in parameters)
        {
            var replacement = entry.Value?.ToString() ?? string.Empty;
            result = ReplaceToken(result, entry.Key, replacement);
        }

        return result;
    }

    private static string ReplaceToken(string template, string key, string replacement)
    {
        var result = template.Replace($"{{{{ {key} }}}}", replacement, StringComparison.Ordinal);
        result = result.Replace($"{{{{{key}}}}}", replacement, StringComparison.Ordinal);
        result = result.Replace($"{{{key}}}", replacement, StringComparison.Ordinal);
        return result;
    }
}
