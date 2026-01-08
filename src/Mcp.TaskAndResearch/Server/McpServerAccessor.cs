using ModelContextProtocol.Server;

namespace Mcp.TaskAndResearch.Server;

internal sealed class McpServerAccessor
{
    public McpServer Server { get; }

    public McpServerAccessor(McpServer server)
    {
        Server = server;
    }
}
