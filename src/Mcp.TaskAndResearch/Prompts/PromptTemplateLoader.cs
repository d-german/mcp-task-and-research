using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Config;
using Mcp.TaskAndResearch.Data;

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

    public Result<string> LoadTemplate(string templatePath)
    {
        var templateSet = GetTemplateSetName();
        var dataDir = _pathResolver.ResolveDataDirectory();
        var customPath = Path.Combine(dataDir, templateSet, templatePath);
        var builtInPath = Path.Combine(GetAssemblyDirectory(), "Prompts", "v1", "templates_en", templatePath);

        if (File.Exists(customPath))
        {
            return Result.Try(() => File.ReadAllText(customPath));
        }

        if (File.Exists(builtInPath))
        {
            return Result.Try(() => File.ReadAllText(builtInPath));
        }

        return Result.Failure<string>(
            new TemplateNotFoundError(templatePath, [customPath, builtInPath]).Message);
    }

    /// <summary>
    /// Loads template and returns content, or throws if template not found.
    /// Use this for built-in templates that must exist.
    /// </summary>
    public string LoadTemplateOrThrow(string templatePath)
    {
        var result = LoadTemplate(templatePath);
        return result.IsSuccess 
            ? result.Value 
            : throw new FileNotFoundException(result.Error);
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
