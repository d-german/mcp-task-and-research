using System.Collections.Immutable;

namespace Mcp.TaskAndResearch.Data;

internal sealed record TaskSearchResult
{
    public required ImmutableArray<TaskItem> Tasks { get; init; }
    public required PaginationInfo Pagination { get; init; }
}

internal sealed record PaginationInfo
{
    public required int CurrentPage { get; init; }
    public required int TotalPages { get; init; }
    public required int TotalResults { get; init; }
    public required bool HasMore { get; init; }
}

internal sealed class TaskSearchService
{
    private readonly TaskStore _taskStore;
    private readonly MemoryStore _memoryStore;

    public TaskSearchService(TaskStore taskStore, MemoryStore memoryStore)
    {
        _taskStore = taskStore;
        _memoryStore = memoryStore;
    }

    public async Task<TaskSearchResult> SearchAsync(string query, bool isId, int page, int pageSize)
    {
        var currentTasks = await _taskStore.GetAllAsync().ConfigureAwait(false);
        var memoryTasks = await _memoryStore.ReadAllSnapshotsAsync().ConfigureAwait(false);

        var filteredCurrent = FilterTasks(currentTasks, query, isId);
        var filteredMemory = FilterTasks(memoryTasks, query, isId);
        var merged = MergeTasks(filteredCurrent, filteredMemory);
        var sorted = SortTasks(merged);

        return Paginate(sorted, page, pageSize);
    }

    private static ImmutableArray<TaskItem> FilterTasks(ImmutableArray<TaskItem> tasks, string query, bool isId)
    {
        if (tasks.IsDefaultOrEmpty)
        {
            return ImmutableArray<TaskItem>.Empty;
        }

        var filtered = ImmutableArray.CreateBuilder<TaskItem>();
        foreach (var task in tasks)
        {
            if (Matches(task, query, isId))
            {
                filtered.Add(task);
            }
        }

        return filtered.ToImmutable();
    }

    private static bool Matches(TaskItem task, string query, bool isId)
    {
        if (isId)
        {
            return task.Id == query;
        }

        var keywords = SplitKeywords(query);
        if (keywords.IsDefaultOrEmpty)
        {
            return true;
        }

        return keywords.All(keyword => MatchesKeyword(task, keyword));
    }

    private static ImmutableArray<string> SplitKeywords(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ImmutableArray<string>.Empty;
        }

        var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return ImmutableArray.CreateRange(parts);
    }

    private static bool MatchesKeyword(TaskItem task, string keyword)
    {
        var value = keyword.ToLowerInvariant();
        return Contains(task.Name, value) ||
               Contains(task.Description, value) ||
               Contains(task.Notes, value) ||
               Contains(task.ImplementationGuide, value) ||
               Contains(task.Summary, value);
    }

    private static bool Contains(string? text, string keyword)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static ImmutableArray<TaskItem> MergeTasks(ImmutableArray<TaskItem> primary, ImmutableArray<TaskItem> secondary)
    {
        var map = new Dictionary<string, TaskItem>();
        foreach (var task in primary)
        {
            map[task.Id] = task;
        }

        foreach (var task in secondary)
        {
            map.TryAdd(task.Id, task);
        }

        return map.Values.ToImmutableArray();
    }

    private static ImmutableArray<TaskItem> SortTasks(ImmutableArray<TaskItem> tasks)
    {
        var list = tasks.ToList();
        list.Sort(CompareTasks);
        return list.ToImmutableArray();
    }

    private static int CompareTasks(TaskItem left, TaskItem right)
    {
        if (left.CompletedAt.HasValue && right.CompletedAt.HasValue)
        {
            return right.CompletedAt.Value.CompareTo(left.CompletedAt.Value);
        }

        if (left.CompletedAt.HasValue)
        {
            return -1;
        }

        if (right.CompletedAt.HasValue)
        {
            return 1;
        }

        return right.UpdatedAt.CompareTo(left.UpdatedAt);
    }

    private static TaskSearchResult Paginate(ImmutableArray<TaskItem> tasks, int page, int pageSize)
    {
        var totalResults = tasks.Length;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalResults / (double)pageSize));
        var currentPage = Math.Clamp(page, 1, totalPages);
        var startIndex = (currentPage - 1) * pageSize;
        var pageTasks = tasks.Skip(startIndex).Take(pageSize).ToImmutableArray();

        return new TaskSearchResult
        {
            Tasks = pageTasks,
            Pagination = new PaginationInfo
            {
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalResults = totalResults,
                HasMore = currentPage < totalPages
            }
        };
    }
}
