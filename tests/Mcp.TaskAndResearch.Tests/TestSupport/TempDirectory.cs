namespace Mcp.TaskAndResearch.Tests.TestSupport;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcp-task-tests");
        Path = System.IO.Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}
