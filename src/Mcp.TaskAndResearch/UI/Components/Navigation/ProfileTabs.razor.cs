using Mcp.TaskAndResearch.UI.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Components.Navigation;

public partial class ProfileTabs : ComponentBase
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<string> OnProfileChanged { get; set; }

    private MudDynamicTabs _tabs = default!;
    private readonly List<ProfileInfo> _profiles = [];
    private bool _showRenameDialog;
    private string _newProfileName = string.Empty;
    private ProfileInfo? _profileToRename;
    private int _profileCounter = 1;

    protected override void OnInitialized()
    {
        // Initialize with a default profile
        _profiles.Add(new ProfileInfo(Guid.NewGuid().ToString(), "Default"));
    }

    private void AddProfile()
    {
        var name = $"Profile {++_profileCounter}";
        var profile = new ProfileInfo(Guid.NewGuid().ToString(), name);
        _profiles.Add(profile);
        StateHasChanged();
    }

    private async Task CloseProfile(MudTabPanel panel)
    {
        if (_profiles.Count <= 1)
        {
            // Don't allow closing the last profile
            return;
        }

        var profile = panel.Tag as ProfileInfo;
        if (profile is null) return;

        var confirmed = await ShowConfirmationDialog(
            "Close Profile",
            $"Are you sure you want to close the profile '{profile.Name}'?");

        if (confirmed)
        {
            _profiles.Remove(profile);
            StateHasChanged();
        }
    }

    private void StartRename(ProfileInfo profile)
    {
        _profileToRename = profile;
        _newProfileName = profile.Name;
        _showRenameDialog = true;
    }

    private void CancelRename()
    {
        _showRenameDialog = false;
        _profileToRename = null;
        _newProfileName = string.Empty;
    }

    private void ConfirmRename()
    {
        if (_profileToRename is not null && !string.IsNullOrWhiteSpace(_newProfileName))
        {
            var index = _profiles.IndexOf(_profileToRename);
            if (index >= 0)
            {
                _profiles[index] = _profileToRename with { Name = _newProfileName };
            }
        }
        
        CancelRename();
        StateHasChanged();
    }

    private async Task<bool> ShowConfirmationDialog(string title, string message)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { nameof(ConfirmDialog.Message), message }
        };

        var options = new DialogOptions { CloseOnEscapeKey = true };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>(title, parameters, options);
        var result = await dialog.Result;

        return result is { Canceled: false };
    }

    private sealed record ProfileInfo(string Id, string Name);
}
