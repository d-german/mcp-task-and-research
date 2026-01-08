namespace Mcp.TaskAndResearch.Data;

internal sealed class RulesStore
{
    private readonly DataPathProvider _pathProvider;

    public RulesStore(DataPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task<string?> ReadAsync()
    {
        var path = _pathProvider.GetPaths().RulesFilePath;
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path).ConfigureAwait(false);
    }

    public async Task WriteAsync(string content)
    {
        var path = _pathProvider.GetPaths().RulesFilePath;
        await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
    }
}
