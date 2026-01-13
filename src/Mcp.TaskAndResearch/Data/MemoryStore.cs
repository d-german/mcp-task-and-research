using System.Collections.Immutable;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Mcp.TaskAndResearch.Extensions;

namespace Mcp.TaskAndResearch.Data;

internal sealed class MemoryStore
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

    public async Task<Result<string>> WriteSnapshotAsync(ImmutableArray<TaskItem> tasks)
    {
        var paths = _pathProvider.GetPaths();
        
        var ensureResult = EnsureDirectory(paths.MemoryDirectory);
        if (ensureResult.IsFailure)
        {
            return Result.Failure<string>(ensureResult.Error);
        }

        var fileName = CreateSnapshotFileName(_timeProvider.GetLocalNow());
        var filePath = Path.Combine(paths.MemoryDirectory, fileName);
        var document = new TaskDocument { Tasks = tasks };

        return await AsyncResultExtensions.TryAsync(async () =>
        {
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, document, _jsonOptions).ConfigureAwait(false);
            return fileName;
        }).ConfigureAwait(false);
    }

    public async Task<Result<ImmutableArray<TaskItem>>> ReadAllSnapshotsAsync()
    {
        var paths = _pathProvider.GetPaths();
        if (!Directory.Exists(paths.MemoryDirectory))
        {
            return Result.Success(ImmutableArray<TaskItem>.Empty);
        }

        var tasks = ImmutableArray.CreateBuilder<TaskItem>();
        foreach (var file in Directory.EnumerateFiles(paths.MemoryDirectory, "*.json"))
        {
            var snapshotResult = await ReadSnapshotFileAsync(file).ConfigureAwait(false);
            if (snapshotResult.IsSuccess)
            {
                tasks.AddRange(snapshotResult.Value);
            }
            // Continue reading other files even if one fails
        }

        return Result.Success(tasks.ToImmutable());
    }

    public async Task<Result<ImmutableArray<TaskItem>>> ReadSnapshotFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result.Failure<ImmutableArray<TaskItem>>($"Snapshot file not found: {filePath}");
        }

        return await AsyncResultExtensions.TryAsync(async () =>
        {
            await using var stream = File.OpenRead(filePath);
            var document = await JsonSerializer.DeserializeAsync<TaskDocument>(stream, _jsonOptions).ConfigureAwait(false);
            return document?.Tasks ?? ImmutableArray<TaskItem>.Empty;
        }).ConfigureAwait(false);
    }

    private static string CreateSnapshotFileName(DateTimeOffset timestamp)
    {
        return $"tasks_memory_{timestamp:yyyyMMddHHmmss}.json";
    }

    private static Result EnsureDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to create directory: {ex.Message}");
        }
    }
}
