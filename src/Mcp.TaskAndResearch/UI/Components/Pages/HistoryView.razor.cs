using System.Collections.Immutable;
using Mcp.TaskAndResearch.Data;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Components.Pages;

public partial class HistoryView : ComponentBase, IDisposable
{
    [Inject]
    private ITaskReader TaskReader { get; set; } = default!;

    [Inject]
    private MemoryStore MemoryStore { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private List<HistoryItem> _historyItems = [];
    private bool _isLoading = true;
    private DateRange? _dateRange;
    private Data.TaskStatus? _statusFilter;

    protected override async Task OnInitializedAsync()
    {
        TaskReader.OnTaskChanged += OnTaskChanged;
        // Default to last 7 days
        _dateRange = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today);
        await LoadHistoryAsync();
    }

    public void Dispose()
    {
        TaskReader.OnTaskChanged -= OnTaskChanged;
        GC.SuppressFinalize(this);
    }

    private void OnTaskChanged(TaskChangeEventArgs args)
    {
        // Re-load history on any change and update UI
        InvokeAsync(async () =>
        {
            await LoadHistoryAsync();
        });
    }

    private async Task LoadHistoryAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            // Load active tasks
            var activeTasks = await TaskReader.GetAllAsync().ConfigureAwait(false);
            
            // Load archived/cleared tasks from snapshots
            var archivedTasks = await MemoryStore.ReadAllSnapshotsAsync().ConfigureAwait(false);
            
            // Merge both, using a HashSet to avoid duplicates (by task ID)
            var seenIds = new HashSet<string>();
            var allTasks = ImmutableArray.CreateBuilder<TaskItem>();
            
            foreach (var task in activeTasks)
            {
                if (seenIds.Add(task.Id))
                {
                    allTasks.Add(task);
                }
            }
            
            foreach (var task in archivedTasks)
            {
                if (seenIds.Add(task.Id))
                {
                    allTasks.Add(task);
                }
            }
            
            _historyItems = BuildHistoryFromTasks(allTasks.ToImmutable(), _dateRange, _statusFilter);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load history: {ex.Message}", Severity.Error);
            _historyItems = [];
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private static List<HistoryItem> BuildHistoryFromTasks(
        ImmutableArray<TaskItem> tasks,
        DateRange? dateRange,
        Data.TaskStatus? statusFilter)
    {
        var items = new List<HistoryItem>();

        foreach (var task in tasks)
        {
            // Add creation event
            if (IsInDateRange(task.CreatedAt, dateRange))
            {
                items.Add(new HistoryItem(
                    task.Id,
                    task.Name,
                    task.CreatedAt,
                    Data.TaskStatus.Pending,
                    "Task created"));
            }

            // Add completion event if completed
            if (task.CompletedAt.HasValue && IsInDateRange(task.CompletedAt.Value, dateRange))
            {
                items.Add(new HistoryItem(
                    task.Id,
                    task.Name,
                    task.CompletedAt.Value,
                    Data.TaskStatus.Completed,
                    task.Summary ?? "Task completed"));
            }

            // Add last update if different from creation
            if (task.UpdatedAt > task.CreatedAt && IsInDateRange(task.UpdatedAt, dateRange))
            {
                items.Add(new HistoryItem(
                    task.Id,
                    task.Name,
                    task.UpdatedAt,
                    task.Status,
                    $"Status: {task.Status}"));
            }
        }

        // Apply status filter
        if (statusFilter.HasValue)
        {
            items = items.Where(i => i.Status == statusFilter.Value).ToList();
        }

        // Sort by timestamp descending (most recent first)
        return [.. items.OrderByDescending(i => i.Timestamp)];
    }

    private static bool IsInDateRange(DateTimeOffset timestamp, DateRange? dateRange)
    {
        if (dateRange?.Start is null || dateRange?.End is null)
            return true;

        var date = timestamp.Date;
        return date >= dateRange.Start.Value.Date && date <= dateRange.End.Value.Date;
    }

    private static Color GetStatusColor(Data.TaskStatus status) => status switch
    {
        Data.TaskStatus.Pending => Color.Warning,
        Data.TaskStatus.InProgress => Color.Info,
        Data.TaskStatus.Completed => Color.Success,
        Data.TaskStatus.Blocked => Color.Error,
        _ => Color.Default
    };

    private static string GetStatusIcon(Data.TaskStatus status) => status switch
    {
        Data.TaskStatus.Pending => Icons.Material.Outlined.Schedule,
        Data.TaskStatus.InProgress => Icons.Material.Outlined.PlayArrow,
        Data.TaskStatus.Completed => Icons.Material.Outlined.CheckCircle,
        Data.TaskStatus.Blocked => Icons.Material.Outlined.Block,
        _ => Icons.Material.Outlined.Help
    };

    private sealed record HistoryItem(
        string TaskId,
        string TaskName,
        DateTimeOffset Timestamp,
        Data.TaskStatus Status,
        string? Summary);
}
