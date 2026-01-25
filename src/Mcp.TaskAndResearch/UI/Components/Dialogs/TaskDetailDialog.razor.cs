using Mcp.TaskAndResearch.Data;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Components.Dialogs;

public partial class TaskDetailDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired]
    public TaskItem Task { get; set; } = default!;

    [Inject]
    private ITaskRepository TaskStore { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ITaskReader TaskReader { get; set; } = default!;

    private MudForm _form = default!;
    private bool _isValid;
    private bool _isEditMode;
    private EditTaskModel _model = new();

    protected override void OnParametersSet()
    {
        _model = CreateModelFromTask(Task);
    }

    private void EnableEdit()
    {
        _isEditMode = true;
    }

    private void Cancel()
    {
        _model = CreateModelFromTask(Task);
        _isEditMode = false;
    }

    private void Close()
    {
        MudDialog.Close(DialogResult.Cancel());
    }

    private async Task Save()
    {
        await _form.Validate();
        
        if (!_isValid) return;

        try
        {
            var request = CreateUpdateRequest(_model);
            var updatedTask = await TaskStore.UpdateAsync(Task.Id, request).ConfigureAwait(false);
            
            Snackbar.Add("Task saved successfully", Severity.Success);
            await InvokeAsync(() => MudDialog.Close(DialogResult.Ok(updatedTask)));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to save task: {ex.Message}", Severity.Error);
        }
    }

    private static EditTaskModel CreateModelFromTask(TaskItem task)
    {
        return new EditTaskModel
        {
            Name = task.Name,
            Description = task.Description,
            Status = task.Status,
            Notes = task.Notes,
            ImplementationGuide = task.ImplementationGuide,
            VerificationCriteria = task.VerificationCriteria
        };
    }

    private static TaskUpdateRequest CreateUpdateRequest(EditTaskModel model)
    {
        return new TaskUpdateRequest
        {
            Name = model.Name,
            Description = model.Description,
            Status = model.Status,
            Notes = model.Notes,
            ImplementationGuide = model.ImplementationGuide,
            VerificationCriteria = model.VerificationCriteria
        };
    }

    private async Task OnDependencyClickedAsync(string taskId)
    {
        var task = await TaskReader.GetByIdAsync(taskId).ConfigureAwait(false);

        if (task is null)
        {
            await InvokeAsync(() =>
            {
                Snackbar.Add("Task not found (may have been deleted)", Severity.Warning);
            });
            return;
        }

        var parameters = new DialogParameters<TaskDetailDialog>
        {
            { x => x.Task, task }
        };

        await InvokeAsync(async () =>
        {
            await DialogService.ShowAsync<TaskDetailDialog>(
                $"Task: {task.Name}",
                parameters,
                new DialogOptions { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true });
        });
    }

    private static Color GetStatusColor(Data.TaskStatus status) => status switch
    {
        Data.TaskStatus.Pending => Color.Default,
        Data.TaskStatus.InProgress => Color.Info,
        Data.TaskStatus.Completed => Color.Success,
        Data.TaskStatus.Blocked => Color.Warning,
        _ => Color.Default
    };

    private sealed class EditTaskModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Data.TaskStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? ImplementationGuide { get; set; }
        public string? VerificationCriteria { get; set; }
    }
}
