using System.Collections.Immutable;
using LiteDB;

namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Snapshot record stored in LiteDB for task history.
/// </summary>
internal sealed record TaskSnapshot
{
    [BsonId]
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required List<TaskItem> Tasks { get; init; }
}

/// <summary>
/// LiteDB implementation of <see cref="IMemoryRepository"/>.
/// Stores task snapshots in a LiteDB collection.
/// </summary>
internal sealed class LiteDbMemoryRepository : IMemoryRepository
{
    private readonly ILiteDbProvider _dbProvider;
    private readonly TimeProvider _timeProvider;
    private const string CollectionName = "snapshots";

    public LiteDbMemoryRepository(ILiteDbProvider dbProvider, TimeProvider? timeProvider = null)
    {
        _dbProvider = dbProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;
        EnsureIndexes();
    }

    private ILiteCollection<TaskSnapshot> Snapshots => _dbProvider.Database.GetCollection<TaskSnapshot>(CollectionName);

    private void EnsureIndexes()
    {
        var snapshots = Snapshots;
        snapshots.EnsureIndex(x => x.Timestamp);
    }

    /// <inheritdoc />
    public Task<string> WriteSnapshotAsync(ImmutableArray<TaskItem> tasks)
    {
        var now = _timeProvider.GetLocalNow();
        var snapshot = new TaskSnapshot
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = now,
            Tasks = tasks.ToList()
        };

        Snapshots.Insert(snapshot);
        return Task.FromResult(snapshot.Id);
    }

    /// <inheritdoc />
    public Task<ImmutableArray<TaskItem>> ReadAllSnapshotsAsync()
    {
        var allSnapshots = Snapshots
            .FindAll()
            .OrderByDescending(s => s.Timestamp)
            .ToList();

        if (allSnapshots.Count == 0)
        {
            return Task.FromResult(ImmutableArray<TaskItem>.Empty);
        }

        var builder = ImmutableArray.CreateBuilder<TaskItem>();
        foreach (var snapshot in allSnapshots)
        {
            builder.AddRange(snapshot.Tasks);
        }

        return Task.FromResult(builder.ToImmutable());
    }
}
