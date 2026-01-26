using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json;
using Mcp.TaskAndResearch.Data;

namespace Mcp.TaskAndResearch.UI.Components.Pages;

public partial class SettingsView : ComponentBase
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IMigrationService MigrationService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private AppSettings _settings = new();
    private bool _isImporting;
    private bool _isExporting;
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

    private async Task ImportLegacyTasks()
    {
        var result = await DialogService.ShowMessageBox(
            "Import Legacy Tasks",
            "This will import tasks from legacy JSON files. Duplicate tasks will be skipped. Continue?",
            yesText: "Import",
            cancelText: "Cancel");

        if (result != true)
        {
            return;
        }

        _isImporting = true;
        StateHasChanged();

        try
        {
            var importResult = await MigrationService.ImportFromJsonAsync();
            
            if (!importResult.Success)
            {
                Snackbar.Add($"Import failed: {importResult.Error}", Severity.Error);
            }
            else if (importResult.TasksImported == 0 && importResult.SnapshotsImported == 0)
            {
                Snackbar.Add("No legacy JSON files found to import", Severity.Info);
            }
            else
            {
                Snackbar.Add($"Imported {importResult.TasksImported} tasks and {importResult.SnapshotsImported} snapshots", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Import error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isImporting = false;
            StateHasChanged();
        }
    }

    private async Task ExportTasksToJson()
    {
        _isExporting = true;
        StateHasChanged();

        try
        {
            var exportResult = await MigrationService.ExportToJsonAsync();
            
            if (!exportResult.Success)
            {
                Snackbar.Add($"Export failed: {exportResult.Error}", Severity.Error);
            }
            else
            {
                Snackbar.Add($"Exported to {exportResult.TasksFilePath}", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isExporting = false;
            StateHasChanged();
        }
    }

    private sealed class AppSettings
    {
        public bool IsDarkMode { get; set; } = true;
        public string Theme { get; set; } = "default";
        public string Language { get; set; } = "en";
public bool ShowNotifications { get; set; } = true;
        public bool PlaySound { get; set; } = false;
    }
}
