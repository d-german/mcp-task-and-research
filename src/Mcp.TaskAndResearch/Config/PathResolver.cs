using System.Collections.Immutable;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace Mcp.TaskAndResearch.Config;

internal sealed class PathResolver
{
    private readonly WorkspaceRootStore _rootStore;
    private readonly ConfigReader _configReader;
    private readonly ILogger<PathResolver> _logger;

    public PathResolver(WorkspaceRootStore rootStore, ConfigReader configReader, ILogger<PathResolver> logger)
    {
        _rootStore = rootStore;
        _configReader = configReader;
        _logger = logger;
    }

    public string ResolveDataDirectory()
    {
        var dataDir = _configReader.GetDataDirectorySetting();
        var usedDefault = false;
        
        if (string.IsNullOrWhiteSpace(dataDir))
        {
            // Use platform-appropriate default for global tool
            dataDir = GetDefaultDataDirectory();
            usedDefault = true;
        }

        string resolvedPath;
        if (Path.IsPathRooted(dataDir))
        {
            resolvedPath = dataDir;
        }
        else
        {
            var workspaceRoot = ResolveWorkspaceRoot();
            resolvedPath = Path.Combine(workspaceRoot, dataDir);
        }

        if (usedDefault)
        {
            _logger.LogInformation("Using default data directory: {DataDirectory}", resolvedPath);
        }

        return resolvedPath;
    }

    private static string GetDefaultDataDirectory()
    {
        // For global tool installation, use a user-specific directory
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "mcp-task-and-research");
        }
        else
        {
            // Linux/Mac: ~/.mcp-task-and-research
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".mcp-task-and-research");
        }
    }

    public string ResolveWorkspaceRoot()
    {
        return GetPreferredRoot()
            .GetValueOrDefault(_configReader.GetWorkspaceRootOverride())
            ?? GetFallbackRoot();
    }

    private Maybe<string> GetPreferredRoot()
    {
        var snapshot = _rootStore.Snapshot;
        var listRoot = FirstNonEmpty(snapshot.ListRoots);
        return listRoot.HasValue ? listRoot : FirstNonEmpty(snapshot.InitializeRoots);
    }

    private static Maybe<string> FirstNonEmpty(ImmutableArray<string> roots)
    {
        foreach (var root in roots)
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
                return Maybe.From(root);
            }
        }

        return Maybe<string>.None;
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

internal sealed record WorkspaceRootSnapshot
{
    public required ImmutableArray<string> ListRoots { get; init; }
    public required ImmutableArray<string> InitializeRoots { get; init; }

    public static WorkspaceRootSnapshot Empty { get; } = new()
    {
        ListRoots = ImmutableArray<string>.Empty,
        InitializeRoots = ImmutableArray<string>.Empty
    };
}

internal sealed class WorkspaceRootStore
{
    private WorkspaceRootSnapshot _snapshot = WorkspaceRootSnapshot.Empty;

    public WorkspaceRootSnapshot Snapshot => _snapshot;

    public void SetListRoots(ImmutableArray<string> listRoots)
    {
        _snapshot = _snapshot with { ListRoots = listRoots };
    }

    public void SetInitializeRoots(ImmutableArray<string> initializeRoots)
    {
        _snapshot = _snapshot with { InitializeRoots = initializeRoots };
    }
}

internal static class WorkspaceRootConverter
{
    public static ImmutableArray<string> FromUris(ImmutableArray<Uri> roots)
    {
        if (roots.IsDefaultOrEmpty)
        {
            return ImmutableArray<string>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var root in roots)
        {
            if (TryGetFilePath(root, out var path))
            {
                builder.Add(path);
            }
        }

        return builder.ToImmutable();
    }

    private static bool TryGetFilePath(Uri uri, out string path)
    {
        if (!uri.IsFile)
        {
            path = string.Empty;
            return false;
        }

        path = Path.GetFullPath(uri.LocalPath);
        return !string.IsNullOrWhiteSpace(path);
    }
}
