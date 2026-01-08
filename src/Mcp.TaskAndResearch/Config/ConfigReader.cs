namespace Mcp.TaskAndResearch.Config;

internal sealed class ConfigReader
{
    private const string DataDirKey = "DATA_DIR";
    private const string WorkspaceRootKey = "MCP_WORKSPACE_ROOT";

    public string? GetDataDirectorySetting()
    {
        return Environment.GetEnvironmentVariable(DataDirKey);
    }

    public string? GetWorkspaceRootOverride()
    {
        return Environment.GetEnvironmentVariable(WorkspaceRootKey);
    }
}
