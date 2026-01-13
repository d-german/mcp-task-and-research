using Mcp.TaskAndResearch.Config;

namespace Mcp.TaskAndResearch.Prompts;

internal sealed class PromptTemplateLoader
{
    private const string DefaultTemplateSet = "en";
    private const string TemplateSetKey = "TEMPLATES_USE";
    private readonly PathResolver _pathResolver;

    public PromptTemplateLoader(PathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public string LoadTemplate(string templatePath)
    {
        var templateSet = GetTemplateSetName();
        var dataDir = _pathResolver.ResolveDataDirectory();
        var customPath = Path.Combine(dataDir, templateSet, templatePath);
        var builtInPath = Path.Combine(GetAssemblyDirectory(), "Prompts", "v1", "templates_en", templatePath);

        if (File.Exists(customPath))
        {
            return File.ReadAllText(customPath);
        }

        if (File.Exists(builtInPath))
        {
            return File.ReadAllText(builtInPath);
        }

        throw new FileNotFoundException(
            $"Template file not found: '{templatePath}'. Checked paths:{Environment.NewLine} - {customPath}{Environment.NewLine} - {builtInPath}");
    }

    private static string GetTemplateSetName()
    {
        return Environment.GetEnvironmentVariable(TemplateSetKey) ?? DefaultTemplateSet;
    }

    private static string GetAssemblyDirectory()
    {
        // AppContext.BaseDirectory works correctly for global .NET tools (unlike Assembly.Location)
        return AppContext.BaseDirectory;
    }
}
