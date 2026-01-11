using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;


namespace Mcp.TaskAndResearch.UI.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private Services.LoadingService _loadingService { get; set; } = default!;

    private bool _isDarkMode = true;
    private bool _drawerOpen = true;
    private bool _isMobileLayout;
    private DrawerVariant _drawerVariant = DrawerVariant.Mini;
    private Breakpoint _currentBreakpoint = Breakpoint.Lg;
    private const string DarkModeKey = "taskManager_darkMode";
    private Shared.AppErrorBoundary? _errorBoundary;

    [Parameter]
    public EventCallback OnRefresh { get; set; }

    protected override void OnInitialized()
    {
        _loadingService.OnChange += StateHasChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadDarkModePreferenceAsync();
            StateHasChanged();
        }
    }

    private void OnBreakpointChanged(Breakpoint breakpoint)
    {
        UpdateLayoutForBreakpoint(breakpoint);
        InvokeAsync(StateHasChanged);
    }

    private void UpdateLayoutForBreakpoint(Breakpoint breakpoint)
    {
        _currentBreakpoint = breakpoint;
        _isMobileLayout = breakpoint <= Breakpoint.Sm;

        if (_isMobileLayout)
        {
            _drawerVariant = DrawerVariant.Temporary;
            _drawerOpen = false;
        }
        else
        {
            _drawerVariant = DrawerVariant.Mini;
            _drawerOpen = true;
        }
    }

    private string GetMainContentClass() =>
        _isMobileLayout ? "pt-14 px-2" : "pt-16 px-4";

    private async Task LoadDarkModePreferenceAsync()
    {
        try
        {
            var stored = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", DarkModeKey);
            if (!string.IsNullOrEmpty(stored) && bool.TryParse(stored, out var isDark))
            {
                _isDarkMode = isDark;
            }
        }
        catch
        {
            // Use default if localStorage fails
        }
    }

    private async Task SaveDarkModePreferenceAsync()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("localStorage.setItem", DarkModeKey, _isDarkMode.ToString().ToLowerInvariant());
        }
        catch
        {
            // Ignore localStorage errors
        }
    }

    private async Task ToggleDarkMode()
    {
        _isDarkMode = !_isDarkMode;
        await SaveDarkModePreferenceAsync();
    }

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task OnRefreshClicked()
    {
        if (OnRefresh.HasDelegate)
        {
            await OnRefresh.InvokeAsync();
        }
    }

    public ValueTask DisposeAsync()
    {
        _loadingService.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
