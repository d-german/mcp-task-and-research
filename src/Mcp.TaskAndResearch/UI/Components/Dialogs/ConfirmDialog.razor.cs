using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Components.Dialogs;

public partial class ConfirmDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = "Are you sure?";

    private void Cancel() => MudDialog.Cancel();

    private void Confirm() => MudDialog.Close(DialogResult.Ok(true));
}
