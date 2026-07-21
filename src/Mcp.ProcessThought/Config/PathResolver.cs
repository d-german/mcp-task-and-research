namespace Mcp.ProcessThought.Config;

/// <summary>
/// Resolves the data directory used to look up user-supplied prompt template overrides.
/// Honors the DATA_DIR environment variable (absolute, or relative to the current directory),
/// falling back to a platform-appropriate per-user directory.
/// </summary>
internal sealed class PathResolver
{
    private const string DataDirKey = "DATA_DIR";

    public string ResolveDataDirectory()
    {
        var dataDir = Environment.GetEnvironmentVariable(DataDirKey);

        if (string.IsNullOrWhiteSpace(dataDir))
        {
            return GetDefaultDataDirectory();
        }

        return Path.IsPathRooted(dataDir)
            ? dataDir
            : Path.Combine(GetFallbackRoot(), dataDir);
    }

    private static string GetDefaultDataDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "mcp-process-thought");
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".mcp-process-thought");
    }

    private static string GetFallbackRoot()
    {
        try
        {
            return Directory.GetCurrentDirectory();
        }
        catch
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
    }
}
