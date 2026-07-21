using System.Text.RegularExpressions;

namespace Mcp.ProcessThought.Prompts;

internal static class PromptTemplateRenderer
{
    private static readonly Regex TokenPattern = new(
        @"\{\{\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*\}\}|\{(?<key>[A-Za-z_][A-Za-z0-9_]*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Render(string template, IReadOnlyDictionary<string, object?>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return template;
        }

        return TokenPattern.Replace(template, match =>
        {
            var key = match.Groups["key"].Value;
            return parameters.TryGetValue(key, out var value)
                ? value?.ToString() ?? string.Empty
                : match.Value;
        });
    }
}
