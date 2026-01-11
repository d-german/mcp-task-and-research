using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Services;

/// <summary>
/// Wrapper service for MudBlazor ISnackbar providing consistent toast notifications across the application.
/// </summary>
public sealed class NotificationService
{
    private readonly ISnackbar _snackbar;

    public NotificationService(ISnackbar snackbar)
    {
        _snackbar = snackbar;
        ConfigureDefaults();
    }

    private void ConfigureDefaults()
    {
        _snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;
        _snackbar.Configuration.PreventDuplicates = false;
        _snackbar.Configuration.NewestOnTop = true;
        _snackbar.Configuration.ShowCloseIcon = true;
        _snackbar.Configuration.VisibleStateDuration = 4000;
        _snackbar.Configuration.HideTransitionDuration = 200;
        _snackbar.Configuration.ShowTransitionDuration = 200;
        _snackbar.Configuration.SnackbarVariant = Variant.Filled;
    }

    public void ShowSuccess(string message, string? title = null) =>
        Show(message, Severity.Success, title);

    public void ShowError(string message, string? title = null) =>
        Show(message, Severity.Error, title, duration: 8000);

    public void ShowWarning(string message, string? title = null) =>
        Show(message, Severity.Warning, title);

    public void ShowInfo(string message, string? title = null) =>
        Show(message, Severity.Info, title);

    public void TaskCreated(string taskName) =>
        ShowSuccess($"Task '{Truncate(taskName, 50)}' created successfully");

    public void TaskUpdated(string taskName) =>
        ShowSuccess($"Task '{Truncate(taskName, 50)}' updated");

    public void TaskDeleted(string taskName) =>
        ShowInfo($"Task '{Truncate(taskName, 50)}' deleted");

    public void TaskCompleted(string taskName) =>
        ShowSuccess($"Task '{Truncate(taskName, 50)}' marked as completed", "✓ Complete");

    public void OperationFailed(string operation, string? details = null) =>
        ShowError(details ?? $"Failed to {operation}", "Operation Failed");

    private void Show(string message, Severity severity, string? title = null, int? duration = null)
    {
        var options = new Action<SnackbarOptions>(config =>
        {
            if (duration.HasValue)
            {
                config.VisibleStateDuration = duration.Value;
            }
        });

        if (!string.IsNullOrEmpty(title))
        {
            _snackbar.Add($"<strong>{title}</strong><br/>{message}", severity, options);
        }
        else
        {
            _snackbar.Add(message, severity, options);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : $"{value[..(maxLength - 3)]}...";
}
