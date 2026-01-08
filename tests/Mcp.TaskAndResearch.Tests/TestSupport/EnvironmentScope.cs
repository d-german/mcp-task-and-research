namespace Mcp.TaskAndResearch.Tests.TestSupport;

internal sealed class EnvironmentScope : IDisposable
{
    private readonly Dictionary<string, string?> _originalValues;

    public EnvironmentScope(IReadOnlyDictionary<string, string?> values)
    {
        _originalValues = values.ToDictionary(
            entry => entry.Key,
            entry => Environment.GetEnvironmentVariable(entry.Key));

        foreach (var entry in values)
        {
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }
    }

    public void Dispose()
    {
        foreach (var entry in _originalValues)
        {
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }
    }
}
