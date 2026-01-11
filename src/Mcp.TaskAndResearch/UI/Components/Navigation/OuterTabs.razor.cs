using Microsoft.AspNetCore.Components;

namespace Mcp.TaskAndResearch.UI.Components.Navigation;

public partial class OuterTabs : ComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public int TaskCount { get; set; }

    [Parameter]
    public int ActiveTabIndex { get; set; }

    [Parameter]
    public EventCallback<int> ActiveTabIndexChanged { get; set; }

    private int _activeIndex;

    protected override void OnParametersSet()
    {
        _activeIndex = ActiveTabIndex;
    }

    private async Task OnActiveIndexChanged(int index)
    {
        _activeIndex = index;
        await ActiveTabIndexChanged.InvokeAsync(index);
    }
}
