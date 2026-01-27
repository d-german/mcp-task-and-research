using Markdig;

namespace Mcp.TaskAndResearch.Services;

/// <summary>
/// Service for converting markdown text to HTML using Markdig.
/// This service provides markdown rendering with consistent configuration across the application.
/// </summary>
public static class MarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    /// <summary>
    /// Converts markdown text to HTML.
    /// Handles newline characters (\n) and markdown syntax (**, *, etc.).
    /// </summary>
    /// <param name="markdown">The markdown text to convert. Can be null or empty.</param>
    /// <returns>HTML string, or empty string if input is null/empty.</returns>
    /// <summary>
    /// Converts markdown text to HTML.
    /// Handles newline characters (\n) and markdown syntax (**, *, etc.).
    /// </summary>
    /// <param name="markdown">The markdown text to convert. Can be null or empty.</param>
    /// <returns>HTML string, or empty string if input is null/empty.</returns>
    /// <summary>
    /// Converts markdown text to HTML.
    /// Handles newline characters (\n) and markdown syntax (**, *, etc.).
    /// </summary>
    /// <param name="markdown">The markdown text to convert. Can be null or empty.</param>
    /// <returns>HTML string, or empty string if input is null/empty.</returns>
    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        // Handle both literal "\n" strings (as two chars: backslash + n) and actual newline chars
        // When LLMs send JSON with \n, it usually arrives as actual newlines after JSON parsing
        // But sometimes the literal characters '\' and 'n' might be double-escaped
        var normalized = markdown;
        
        // Check for literal backslash-n sequences (would appear as \\n in a debugger)
        if (normalized.Contains(@"\n"))
        {
            normalized = normalized.Replace(@"\n", "\n");
        }
        
        // Also handle other common escape sequences that might be literally present
        if (normalized.Contains(@"\r\n"))
        {
            normalized = normalized.Replace(@"\r\n", "\n");
        }
        
        if (normalized.Contains(@"\r"))
        {
            normalized = normalized.Replace(@"\r", "\r");
        }
        
        if (normalized.Contains(@"\t"))
        {
            normalized = normalized.Replace(@"\t", "\t");
        }

        return Markdown.ToHtml(normalized, Pipeline);
    }

    /// <summary>
    /// Converts markdown to HTML and applies search term highlighting with &lt;mark&gt; tags.
    /// XSS-safe: Markdown is rendered to HTML first, then highlighting is applied to the result.
    /// </summary>
    /// <param name="markdown">The markdown text to convert and highlight.</param>
    /// <param name="searchTerm">Optional search term to highlight in the rendered HTML.</param>
    /// <returns>HTML string with search terms highlighted.</returns>
    public static string ToHtmlWithHighlighting(string? markdown, string? searchTerm)
    {
        var html = ToHtml(markdown);

        if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(searchTerm))
        {
            return html;
        }

        return HighlightSearchTerm(html, searchTerm);
    }

    private static string HighlightSearchTerm(string html, string searchTerm)
    {
        var result = new System.Text.StringBuilder();
        var currentIndex = 0;

        while (currentIndex < html.Length)
        {
            var index = html.IndexOf(searchTerm, currentIndex, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                // No more matches - add rest of HTML
                result.Append(html.Substring(currentIndex));
                break;
            }

            // Add HTML before match
            if (index > currentIndex)
            {
                result.Append(html.Substring(currentIndex, index - currentIndex));
            }

            // Add highlighted match (already HTML-safe since it's from rendered markdown)
            var actualMatch = html.Substring(index, searchTerm.Length);
            result.Append("<mark>");
            result.Append(actualMatch);
            result.Append("</mark>");

            currentIndex = index + searchTerm.Length;
        }

        return result.ToString();
    }
}
