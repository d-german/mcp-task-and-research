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
    private TaskStore TaskStore { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

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
            var updateResult = await TaskStore.UpdateAsync(Task.Id, request).ConfigureAwait(false);
            
            if (updateResult.IsSuccess)
            {
                Snackbar.Add("Task saved successfully", Severity.Success);
                await InvokeAsync(() => MudDialog.Close(DialogResult.Ok(updateResult.Value)));
            }
            else
            {
                Snackbar.Add($"Failed to save task: {updateResult.Error}", Severity.Error);
            }
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
