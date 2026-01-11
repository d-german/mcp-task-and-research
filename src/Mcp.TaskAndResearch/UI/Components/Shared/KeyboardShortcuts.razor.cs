using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Components.Shared;

public partial class KeyboardShortcuts : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public EventCallback OnNewTask { get; set; }

    [Parameter]
    public EventCallback OnSave { get; set; }

    [Parameter]
    public EventCallback OnRefresh { get; set; }

    [Parameter]
    public string SearchInputSelector { get; set; } = ".search-field input";

    private DotNetObjectReference<KeyboardShortcuts>? _dotNetRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("keyboardShortcuts.init", _dotNetRef);
        }
    }

    [JSInvokable]
    public async Task OnNewTaskInvoked()
    {
        if (OnNewTask.HasDelegate)
        {
            await OnNewTask.InvokeAsync();
        }
        else
        {
            // Navigate to tasks page if no handler
            NavigationManager.NavigateTo("/tasks");
        }
    }

    [JSInvokable]
    public async Task OnFocusSearch()
    {
        await JsRuntime.InvokeVoidAsync("keyboardShortcuts.focusElement", SearchInputSelector);
    }

    [JSInvokable]
    public async Task OnSaveInvoked()
    {
        if (OnSave.HasDelegate)
        {
            await OnSave.InvokeAsync();
        }
    }

    [JSInvokable]
    public async Task OnEscape()
    {
        // MudBlazor dialogs handle Escape automatically via CloseOnEscapeKey option
        await Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnRefreshInvoked()
    {
        if (OnRefresh.HasDelegate)
        {
            await OnRefresh.InvokeAsync();
        }
    }

    [JSInvokable]
    public async Task OnShowHelp()
    {
        await ShowHelpDialogAsync();
    }

    private async Task ShowHelpDialogAsync()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        await DialogService.ShowMessageBox(
            "Keyboard Shortcuts",
            (MarkupString)"""
            <table style="width: 100%;">
                <tr><td><kbd>Ctrl</kbd> + <kbd>N</kbd></td><td>New Task</td></tr>
                <tr><td><kbd>Ctrl</kbd> + <kbd>F</kbd></td><td>Focus Search</td></tr>
                <tr><td><kbd>Ctrl</kbd> + <kbd>S</kbd></td><td>Save</td></tr>
                <tr><td><kbd>Ctrl</kbd> + <kbd>R</kbd></td><td>Refresh</td></tr>
                <tr><td><kbd>Esc</kbd></td><td>Close Dialog</td></tr>
                <tr><td><kbd>?</kbd></td><td>Show This Help</td></tr>
            </table>
            """,
            yesText: "Close",
            options: options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef is not null)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("keyboardShortcuts.dispose");
            }
            catch
            {
                // Ignore if JS runtime is not available during dispose
            }
            _dotNetRef.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
