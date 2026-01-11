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

    private string _searchString = string.Empty;

    protected override void OnParametersSet()
    {
        _searchString = SearchString;
    }

    public void Dispose()
    {
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


}
