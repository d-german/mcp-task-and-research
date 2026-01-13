using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Extensions;

namespace Mcp.TaskAndResearch.Data;

internal sealed class RulesStore
{
    private readonly DataPathProvider _pathProvider;

    public RulesStore(DataPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    /// <summary>
    /// Reads the project rules file content.
    /// </summary>
    /// <returns>Maybe with content if file exists, Maybe.None if not found.</returns>
    public async Task<Maybe<string>> ReadAsync()
    {
        var path = _pathProvider.GetPaths().RulesFilePath;
        if (!File.Exists(path))
        {
            return Maybe<string>.None;
        }

        var result = await AsyncResultExtensions.TryAsync(async () =>
        {
            return await File.ReadAllTextAsync(path).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return result.IsSuccess ? Maybe<string>.From(result.Value) : Maybe<string>.None;
    }

    /// <summary>
    /// Writes content to the project rules file.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <returns>Result indicating success or failure.</returns>
    public async Task<Result> WriteAsync(string content)
    {
        var path = _pathProvider.GetPaths().RulesFilePath;
        
        return await AsyncResultExtensions.TryAsync(async () =>
        {
            await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}
