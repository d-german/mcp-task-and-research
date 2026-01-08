using System.Collections.Immutable;

namespace Mcp.TaskAndResearch.Config;

internal sealed class PathResolver
{
    private readonly WorkspaceRootStore _rootStore;
    private readonly ConfigReader _configReader;

    public PathResolver(WorkspaceRootStore rootStore, ConfigReader configReader)
    {
        _rootStore = rootStore;
        _configReader = configReader;
    }

    public string ResolveDataDirectory()
    {
        var dataDir = _configReader.GetDataDirectorySetting();
        if (string.IsNullOrWhiteSpace(dataDir))
        {
            dataDir = "data";
        }

        if (Path.IsPathRooted(dataDir))
        {
            return dataDir;
        }

        var workspaceRoot = ResolveWorkspaceRoot();
        return Path.Combine(workspaceRoot, dataDir);
    }

    public string ResolveWorkspaceRoot()
    {
        return GetPreferredRoot()
            ?? _configReader.GetWorkspaceRootOverride()
            ?? GetFallbackRoot();
    }

    private string? GetPreferredRoot()
    {
        var snapshot = _rootStore.Snapshot;
        return FirstNonEmpty(snapshot.ListRoots) ?? FirstNonEmpty(snapshot.InitializeRoots);
    }

    private static string? FirstNonEmpty(ImmutableArray<string> roots)
    {
        foreach (var root in roots)
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
                return root;
            }
        }

        return null;
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
