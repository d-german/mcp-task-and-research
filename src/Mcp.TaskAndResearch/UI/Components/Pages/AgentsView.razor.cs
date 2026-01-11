using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Components.Pages;

public partial class AgentsView : ComponentBase
{
    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private List<AgentInfo> _agents = [];
    private MudForm _form = default!;
    private bool _isValid;
    private bool _showDialog;
    private bool _isEditMode;
    private AgentEditModel _editModel = new();
    private AgentInfo? _editingAgent;

    protected override void OnInitialized()
    {
        // Initialize with some default agents
        _agents =
        [
            new AgentInfo(Guid.NewGuid().ToString(), "Default Agent", "General purpose assistant", "claude-3-sonnet", null),
            new AgentInfo(Guid.NewGuid().ToString(), "Code Reviewer", "Specialized in code review", "claude-3-opus", "You are an expert code reviewer..."),
        ];
    }

    private void OpenAddDialog()
    {
        _isEditMode = false;
        _editModel = new AgentEditModel();
        _editingAgent = null;
        _showDialog = true;
    }

    private void OpenEditDialog(AgentInfo agent)
    {
        _isEditMode = true;
        _editingAgent = agent;
        _editModel = new AgentEditModel
        {
            Name = agent.Name,
            Description = agent.Description,
            Model = agent.Model,
            SystemPrompt = agent.SystemPrompt
        };
        _showDialog = true;
    }

    private void CloseDialog()
    {
        _showDialog = false;
        _editModel = new AgentEditModel();
        _editingAgent = null;
    }

    private async Task SaveAgent()
    {
        await _form.Validate();
        if (!_isValid) return;

        if (_isEditMode && _editingAgent is not null)
        {
            // Update existing
            var index = _agents.FindIndex(a => a.Id == _editingAgent.Id);
            if (index >= 0)
            {
                _agents[index] = CreateAgentFromModel(_editingAgent.Id, _editModel);
            }
            Snackbar.Add("Agent updated successfully", Severity.Success);
        }
        else
        {
            // Add new
            var agent = CreateAgentFromModel(Guid.NewGuid().ToString(), _editModel);
            _agents.Add(agent);
            Snackbar.Add("Agent added successfully", Severity.Success);
        }

        CloseDialog();
    }

    private async Task DeleteAgent(AgentInfo agent)
    {
        var parameters = new DialogParameters<Dialogs.ConfirmDialog>
        {
            { nameof(Dialogs.ConfirmDialog.Message), $"Are you sure you want to delete the agent '{agent.Name}'?" }
        };

        var options = new DialogOptions { CloseOnEscapeKey = true };
        var dialog = await DialogService.ShowAsync<Dialogs.ConfirmDialog>("Delete Agent", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            _agents.Remove(agent);
            Snackbar.Add("Agent deleted", Severity.Info);
        }
    }

    private static AgentInfo CreateAgentFromModel(string id, AgentEditModel model)
    {
        return new AgentInfo(
            id,
            model.Name,
            model.Description,
            model.Model,
            string.IsNullOrWhiteSpace(model.SystemPrompt) ? null : model.SystemPrompt);
    }

    private sealed record AgentInfo(
        string Id,
        string Name,
        string? Description,
        string Model,
        string? SystemPrompt);

    private sealed class AgentEditModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Model { get; set; } = "claude-3-sonnet";
        public string? SystemPrompt { get; set; }
    }
}
