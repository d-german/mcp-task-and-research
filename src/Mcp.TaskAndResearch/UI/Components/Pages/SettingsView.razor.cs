using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json;

namespace Mcp.TaskAndResearch.UI.Components.Pages;

public partial class SettingsView : ComponentBase
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private AppSettings _settings = new();
    private const string SettingsKey = "taskManager_settings";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadSettingsAsync();
            StateHasChanged();
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var json = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", SettingsKey);
            if (!string.IsNullOrEmpty(json))
            {
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    private async Task SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings);
            await JsRuntime.InvokeVoidAsync("localStorage.setItem", SettingsKey, json);
            Snackbar.Add("Settings saved successfully", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to save settings: {ex.Message}", Severity.Error);
        }
    }

    private async Task ExportSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", json);
            Snackbar.Add("Settings copied to clipboard", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to export: {ex.Message}", Severity.Error);
        }
    }

    private async Task ImportSettings()
    {
        try
        {
            var json = await JsRuntime.InvokeAsync<string>("navigator.clipboard.readText");
            if (!string.IsNullOrEmpty(json))
            {
                var imported = JsonSerializer.Deserialize<AppSettings>(json);
                if (imported is not null)
                {
                    _settings = imported;
                    await SaveSettings();
                    Snackbar.Add("Settings imported successfully", Severity.Success);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to import: {ex.Message}", Severity.Error);
        }
    }

    private async Task ResetToDefaults()
    {
        _settings = new AppSettings();
        await SaveSettings();
        Snackbar.Add("Settings reset to defaults", Severity.Info);
    }

    private sealed class AppSettings
    {
        public bool IsDarkMode { get; set; } = true;
        public string Theme { get; set; } = "default";
        public string Language { get; set; } = "en";
        public bool AutoRefreshEnabled { get; set; } = false;
        public int AutoRefreshInterval { get; set; } = 30;
        public bool ShowNotifications { get; set; } = true;
        public bool PlaySound { get; set; } = false;
    }
}
