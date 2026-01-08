using Mcp.TaskAndResearch.Config;

namespace Mcp.TaskAndResearch.Data;

internal sealed record DataPaths
{
    public required string DataDirectory { get; init; }
    public required string TasksFilePath { get; init; }
    public required string MemoryDirectory { get; init; }
    public required string RulesFilePath { get; init; }
}

internal sealed class DataPathProvider
{
    private readonly PathResolver _pathResolver;

    public DataPathProvider(PathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public DataPaths GetPaths()
    {
        var dataDirectory = _pathResolver.ResolveDataDirectory();
        var memoryDirectory = Path.Combine(dataDirectory, "memory");
        var tasksFilePath = Path.Combine(dataDirectory, "tasks.json");
        var rulesFilePath = Path.Combine(_pathResolver.ResolveWorkspaceRoot(), "shrimp-rules.md");

        return new DataPaths
        {
            DataDirectory = dataDirectory,
            MemoryDirectory = memoryDirectory,
            TasksFilePath = tasksFilePath,
            RulesFilePath = rulesFilePath
        };
    }
}
