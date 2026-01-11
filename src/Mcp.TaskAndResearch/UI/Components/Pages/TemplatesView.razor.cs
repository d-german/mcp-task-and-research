using Mcp.TaskAndResearch.Prompts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Mcp.TaskAndResearch.UI.Components.Pages;

public partial class TemplatesView : ComponentBase
{
    [Inject]
    private PromptTemplateLoader TemplateLoader { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private List<TemplateInfo> _templates = [];
    private bool _isLoading = true;
    private string _searchString = string.Empty;
    private bool _showDialog;
    private bool _isEditMode;
    private TemplateInfo? _selectedTemplate;
    private string _templateContent = string.Empty;

    private readonly DialogOptions _dialogOptions = new()
    {
        MaxWidth = MaxWidth.Large,
        FullWidth = true
    };

    private Func<TemplateInfo, bool> _quickFilter => template =>
    {
        if (string.IsNullOrWhiteSpace(_searchString))
            return true;

        return MatchesSearch(template, _searchString);
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplatesAsync();
    }

    private async Task LoadTemplatesAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            // Load templates from the PromptTemplateLoader
            _templates = await LoadTemplatesFromFilesAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load templates: {ex.Message}", Severity.Error);
            _templates = [];
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task<List<TemplateInfo>> LoadTemplatesFromFilesAsync()
    {
        var templates = new List<TemplateInfo>();
        
        // Get template files from the Prompts directory
        var promptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts");
        
        if (!Directory.Exists(promptsPath))
        {
            return templates;
        }

        await Task.Run(() =>
        {
            var files = Directory.GetFiles(promptsPath, "*.liquid", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var content = File.ReadAllText(file);
                var relativePath = Path.GetRelativePath(promptsPath, file);
                var category = Path.GetDirectoryName(relativePath) ?? "General";
                var name = Path.GetFileNameWithoutExtension(file);
                
                templates.Add(new TemplateInfo(
                    file,
                    name,
                    category,
                    CreatePreview(content),
                    content));
            }
        });

        return [.. templates.OrderBy(t => t.Category).ThenBy(t => t.Name)];
    }

    private void ViewTemplate(TemplateInfo template)
    {
        _selectedTemplate = template;
        _templateContent = template.Content;
        _isEditMode = false;
        _showDialog = true;
    }

    private void EditTemplate(TemplateInfo template)
    {
        _selectedTemplate = template;
        _templateContent = template.Content;
        _isEditMode = true;
        _showDialog = true;
    }

    private async Task SaveTemplate()
    {
        if (_selectedTemplate is null) return;

        try
        {
            await File.WriteAllTextAsync(_selectedTemplate.FilePath, _templateContent);
            
            // Update the template in the list
            var index = _templates.FindIndex(t => t.FilePath == _selectedTemplate.FilePath);
            if (index >= 0)
            {
                _templates[index] = _selectedTemplate with
                {
                    Content = _templateContent,
                    Preview = CreatePreview(_templateContent)
                };
            }

            Snackbar.Add("Template saved successfully", Severity.Success);
            CloseDialog();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to save template: {ex.Message}", Severity.Error);
        }
    }

    private void CloseDialog()
    {
        _showDialog = false;
        _selectedTemplate = null;
        _templateContent = string.Empty;
        _isEditMode = false;
    }

    private static bool MatchesSearch(TemplateInfo template, string search)
    {
        return template.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               template.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
               template.Content.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreatePreview(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Remove template tags and get first meaningful content
        var cleaned = content
            .Replace("{{", "")
            .Replace("}}", "")
            .Replace("{%", "")
            .Replace("%}", "");

        var lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var preview = string.Join(" ", lines.Take(2)).Trim();

        return preview.Length > 100 ? preview[..100] + "..." : preview;
    }

    private sealed record TemplateInfo(
        string FilePath,
        string Name,
        string Category,
        string Preview,
        string Content);
}
