using System.Collections.Immutable;
using System.Text.Json;

namespace Mcp.TaskAndResearch.Data;

internal sealed class MemoryStore : IMemoryRepository
{
    private readonly DataPathProvider _pathProvider;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeProvider _timeProvider;

    public MemoryStore(DataPathProvider pathProvider, TimeProvider? timeProvider = null)
    {
        _pathProvider = pathProvider;
        _jsonOptions = JsonSettings.Default;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<string> WriteSnapshotAsync(ImmutableArray<TaskItem> tasks)
    {
        var paths = _pathProvider.GetPaths();
        EnsureDirectory(paths.MemoryDirectory);

        var fileName = CreateSnapshotFileName(_timeProvider.GetLocalNow());
        var filePath = Path.Combine(paths.MemoryDirectory, fileName);
        var document = new TaskDocument { Tasks = tasks };

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, _jsonOptions).ConfigureAwait(false);

        return fileName;
    }

    public async Task<ImmutableArray<TaskItem>> ReadAllSnapshotsAsync()
    {
        var paths = _pathProvider.GetPaths();
        if (!Directory.Exists(paths.MemoryDirectory))
        {
            return ImmutableArray<TaskItem>.Empty;
        }

        var tasks = ImmutableArray.CreateBuilder<TaskItem>();
        foreach (var file in Directory.EnumerateFiles(paths.MemoryDirectory, "*.json"))
        {
            var snapshotTasks = await ReadSnapshotFileAsync(file).ConfigureAwait(false);
            tasks.AddRange(snapshotTasks);
        }

        return tasks.ToImmutable();
    }

    private async Task<ImmutableArray<TaskItem>> ReadSnapshotFileAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var document = await JsonSerializer.DeserializeAsync<TaskDocument>(stream, _jsonOptions).ConfigureAwait(false);
        return document?.Tasks ?? ImmutableArray<TaskItem>.Empty;
    }

    private static string CreateSnapshotFileName(DateTimeOffset timestamp)
    {
        return $"tasks_memory_{timestamp:yyyyMMddHHmmss}.json";
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
