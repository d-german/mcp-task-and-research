using FuzzySharp;
using Mcp.TaskAndResearch.Data;

namespace Mcp.TaskAndResearch.UI.Services;

/// <summary>
/// Service to provide fuzzy string matching for task searches.
/// Uses the FuzzySharp library with Levenshtein distance algorithm.
/// </summary>
public static class FuzzySearchService
{
    private const int DefaultThreshold = 60;

    /// <summary>
    /// Determines whether a task matches the given search text using fuzzy string matching.
    /// </summary>
    /// <param name="task">The task to search.</param>
    /// <param name="searchText">The text to search for. Empty or whitespace returns true.</param>
    /// <param name="threshold">Minimum similarity score (0-100) required for a match. Default is 60.</param>
    /// <returns>True if any searchable field matches the search text above the threshold.</returns>
    public static bool MatchesTaskSearch(TaskItem task, string searchText, int threshold = DefaultThreshold)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var normalizedSearch = searchText.Trim();
        var searchableFields = GetSearchableFields(task);

        return searchableFields.Any(field => FuzzyContains(field, normalizedSearch, threshold));
    }

    /// <summary>
    /// Gets all searchable text fields from a task item.
    /// </summary>
    /// <param name="task">The task to extract fields from.</param>
    /// <returns>Array of non-null searchable field values.</returns>
    private static string[] GetSearchableFields(TaskItem task)
    {
        return
        [
            task.Name,
            task.Description,
            task.Notes ?? string.Empty,
            task.Summary ?? string.Empty,
            task.AnalysisResult ?? string.Empty,
            task.ImplementationGuide ?? string.Empty,
            task.VerificationCriteria ?? string.Empty,
            task.Agent ?? string.Empty
        ];
    }

    /// <summary>
    /// Determines whether the text contains the search string using fuzzy matching.
    /// </summary>
    /// <param name="text">The text to search in.</param>
    /// <param name="search">The search string.</param>
    /// <param name="threshold">Minimum similarity score (0-100) required for a match.</param>
    /// <returns>True if the fuzzy match score exceeds the threshold.</returns>
    private static bool FuzzyContains(string text, string search, int threshold)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        // Use PartialRatio for substring matching - ideal for searching within larger text
        var score = Fuzz.PartialRatio(text.ToLowerInvariant(), search.ToLowerInvariant());
        return score >= threshold;
    }
}
