namespace Mcp.TaskAndResearch.Server;

internal sealed record ServerMetadata(string Name, string Version)
{
    public static ServerMetadata Default { get; } = new("McpTaskAndResearch", "0.1.0");
}
