using System.ComponentModel;
using ModelContextProtocol.Server;
using Mcp.TaskAndResearch.Server;

namespace Mcp.TaskAndResearch.Tools;

[McpServerToolType]
internal static class ServerMetadataTools
{
    [McpServerTool, Description("Returns basic server metadata.")]
    public static string GetServerInfo(ServerMetadata metadata)
    {
        return $"name: {metadata.Name}, version: {metadata.Version}";
    }
}
