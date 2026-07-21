namespace Mcp.ProcessThought.Prompts;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(thought);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        if (thoughtNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(thoughtNumber), thoughtNumber, "Thought number must be at least 1.");
        }

        if (totalThoughts < thoughtNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(totalThoughts), totalThoughts, "Total thoughts must be at least the current thought number.");
        }

        thought = thought.Trim();
        stage = stage.Trim();
        tags = Normalize(tags);
        axiomsUsed = Normalize(axiomsUsed);
        assumptionsChallenged = Normalize(assumptionsChallenged);

        var nextThoughtTemplate = nextThoughtNeeded
            ? _templateLoader.LoadTemplate("processThought/moreThought.md")
            : _templateLoader.LoadTemplate("processThought/complatedThought.md");
        nextThoughtTemplate = nextThoughtTemplate.Trim();
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
            ["metadata"] = BuildMetadata(tags, axiomsUsed, assumptionsChallenged),
            ["nextThoughtNeeded"] = nextThoughtTemplate
        });

        return PromptCustomization.Apply(prompt.TrimEnd(), "PROCESS_THOUGHT");
    }

    /// <summary>
    /// Builds the optional metadata block, omitting any field the caller left empty so that
    /// zero-information filler lines (e.g. "Tags: no tags") never reach the model. Returns an
    /// empty string when no metadata was supplied, otherwise the lines plus a trailing blank line.
    /// </summary>
    private static string BuildMetadata(string[] tags, string[] axiomsUsed, string[] assumptionsChallenged)
    {
        var lines = new List<string>(3);
        AppendLine(lines, "Tags", tags);
        AppendLine(lines, "Principles Used", axiomsUsed);
        AppendLine(lines, "Assumptions Challenged", assumptionsChallenged);

        return lines.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, lines) + Environment.NewLine + Environment.NewLine;
    }

    private static void AppendLine(List<string> lines, string label, string[] values)
    {
        if (values.Length > 0)
        {
            lines.Add($"**{label}:** {string.Join(", ", values)}");
        }
    }

    private static string FormatList(string[] values, string fallback)
    {
        return values.Length == 0 ? fallback : string.Join(", ", values);
    }

    private static string[] Normalize(string[] values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
