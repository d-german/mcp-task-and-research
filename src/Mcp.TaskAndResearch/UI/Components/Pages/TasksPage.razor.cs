using System.Collections.Immutable;
using Mcp.TaskAndResearch.Data;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Components.Pages;

public partial class TasksPage : ComponentBase, IDisposable
{
    [Inject] private ITaskReader TaskReader { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private Services.LoadingService LoadingService { get; set; } = default!;
    [Inject] private ILogger<TasksPage> Logger { get; set; } = default!;

    private ImmutableArray<TaskItem> _tasks = [];
    private bool _isLoading = true;
    private string _searchString = string.Empty;
private Func<TaskItem, bool> _quickFilter => task =>
    {
        if (string.IsNullOrWhiteSpace(_searchString))
            return true;
            
        return MatchesSearch(task, _searchString);
    };

    protected override async Task OnInitializedAsync()
    {
        TaskReader.OnTaskChanged += OnTaskChanged;
        await LoadTasksAsync();
    }

    public void Dispose()
    {
        TaskReader.OnTaskChanged -= OnTaskChanged;
        GC.SuppressFinalize(this);
    }

    private void OnTaskChanged(TaskChangeEventArgs args)
    {
        // Re-load tasks on any change and update UI
        InvokeAsync(async () =>
        {
            await LoadTasksAsync();
        });
    }

    private async Task LoadTasksAsync()
    {
        _isLoading = true;
        StateHasChanged();
        
        try
        {
            _tasks = await TaskReader.GetAllAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load tasks");
            Snackbar.Add($"Failed to load tasks: {ex.Message}", Severity.Error);
            _tasks = [];
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void OnRowClick(DataGridRowClickEventArgs<TaskItem> args)
    {
        // Navigate to task detail or open dialog
        _ = ViewTaskAsync(args.Item);
    }

    private async Task ViewTaskAsync(TaskItem task)
    {
        await OpenTaskDialogAsync(task, false);
    }

    private async Task EditTaskAsync(TaskItem task)
    {
        await OpenTaskDialogAsync(task, true);
    }

    private async Task OpenTaskDialogAsync(TaskItem task, bool startInEditMode)
    {
        try
        {
            var parameters = new DialogParameters<Dialogs.TaskDetailDialog>
            {
                { x => x.Task, task }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseOnEscapeKey = true
            };

            var dialog = await DialogService.ShowAsync<Dialogs.TaskDetailDialog>(
                startInEditMode ? "Edit Task" : "Task Details", 
                parameters, 
                options);
            var result = await dialog.Result;

            if (result is { Canceled: false })
            {
                await LoadTasksAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error opening task dialog for task {TaskId}", task.Id);
            Snackbar.Add("Failed to open task dialog", Severity.Error);
        }
    }

    private static bool MatchesSearch(TaskItem task, string search)
    {
        return Services.FuzzySearchService.MatchesTaskSearch(task, search);
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

    private static string FormatRelativeTime(DateTimeOffset time)
    {
        var diff = DateTimeOffset.Now - time;
        
        return diff.TotalMinutes switch
        {
            < 1 => "just now",
            < 60 => $"{(int)diff.TotalMinutes}m ago",
            < 1440 => $"{(int)diff.TotalHours}h ago",
            < 10080 => $"{(int)diff.TotalDays}d ago",
            _ => time.ToString("MMM d")
        };
    }
}
