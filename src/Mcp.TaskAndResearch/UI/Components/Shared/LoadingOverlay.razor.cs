using Microsoft.AspNetCore.Components;

namespace Mcp.TaskAndResearch.UI.Components.Shared;

public partial class LoadingOverlay : ComponentBase
{
    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string? Message { get; set; }
}
