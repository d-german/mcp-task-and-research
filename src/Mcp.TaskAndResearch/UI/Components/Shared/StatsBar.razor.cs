using Microsoft.AspNetCore.Components;

namespace Mcp.TaskAndResearch.UI.Components.Shared;

public partial class StatsBar : ComponentBase, IDisposable
{
    [Parameter]
    public int TotalCount { get; set; }

    [Parameter]
    public int PendingCount { get; set; }

    [Parameter]
    public int InProgressCount { get; set; }

    [Parameter]
    public int CompletedCount { get; set; }

    [Parameter]
    public int BlockedCount { get; set; }

    [Parameter]
    public string SearchString { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> SearchStringChanged { get; set; }

    [Parameter]
    public EventCallback OnRefresh { get; set; }

    [Parameter]
    public bool AutoRefreshEnabled { get; set; }

    [Parameter]
    public EventCallback<bool> AutoRefreshEnabledChanged { get; set; }

    [Parameter]
    public int AutoRefreshInterval { get; set; } = 30;

    private string _searchString = string.Empty;
    private System.Timers.Timer? _refreshTimer;

    protected override void OnParametersSet()
    {
        _searchString = SearchString;
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (AutoRefreshEnabled && AutoRefreshInterval > 0)
        {
            _refreshTimer?.Stop();
            _refreshTimer = new System.Timers.Timer(AutoRefreshInterval * 1000);
            _refreshTimer.Elapsed += OnTimerElapsed;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
        }
        else
        {
            StopTimer();
        }
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        InvokeAsync(async () =>
        {
            if (OnRefresh.HasDelegate)
            {
                await OnRefresh.InvokeAsync();
            }
        });
    }

    private void StopTimer()
    {
        if (_refreshTimer is not null)
        {
            _refreshTimer.Elapsed -= OnTimerElapsed;
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _refreshTimer = null;
        }
    }

    public void Dispose()
    {
        StopTimer();
        GC.SuppressFinalize(this);
    }

    private async Task OnSearchChanged(string value)
    {
        _searchString = value;
        await SearchStringChanged.InvokeAsync(value);
    }

    private async Task OnRefreshClicked()
    {
        if (OnRefresh.HasDelegate)
        {
            await OnRefresh.InvokeAsync();
        }
    }

    private async Task OnAutoRefreshToggled(bool value)
    {
        if (AutoRefreshEnabledChanged.HasDelegate)
        {
            await AutoRefreshEnabledChanged.InvokeAsync(value);
        }
        UpdateTimer();
    }
}
