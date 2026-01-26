using LiteDB;

namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Provides a singleton LiteDB database instance for the application.
/// Manages database lifecycle and configuration.
/// </summary>
public interface ILiteDbProvider : IDisposable
{
    /// <summary>
    /// Gets the LiteDB database instance.
    /// </summary>
    LiteDatabase Database { get; }
}

/// <summary>
/// Default implementation of <see cref="ILiteDbProvider"/> that manages
/// a single LiteDB database file in the application data directory.
/// </summary>
internal sealed class LiteDbProvider : ILiteDbProvider
{
    private readonly Lazy<LiteDatabase> _database;
    private bool _disposed;

    public LiteDbProvider(DataPathProvider pathProvider)
    {
        var paths = pathProvider.GetPaths();
        EnsureDirectory(paths.DataDirectory);

        var dbPath = Path.Combine(paths.DataDirectory, "tasks.db");
        var connectionString = BuildConnectionString(dbPath);

        _database = new Lazy<LiteDatabase>(() => CreateDatabase(connectionString));
    }

    public LiteDatabase Database => _database.Value;

    private static LiteDatabase CreateDatabase(string connectionString)
    {
        var db = new LiteDatabase(connectionString);
        ConfigureMapper(db.Mapper);
        return db;
    }

    private static string BuildConnectionString(string dbPath)
    {
        // Use Exclusive mode for better performance (single process access)
        // and enable UTC date handling for consistency
        return $"Filename={dbPath};Connection=Shared;";
    }

    private static void ConfigureMapper(BsonMapper mapper)
    {
        // Configure DateTimeOffset serialization
        // Store as UTC, read back directly (LiteDB handles UTC/local conversion)
        mapper.RegisterType<DateTimeOffset>(
            serialize: (DateTimeOffset dto) => new BsonValue(dto.UtcDateTime),
            deserialize: (BsonValue bson) => new DateTimeOffset(bson.AsDateTime));

        // Configure nullable DateTimeOffset serialization
        mapper.RegisterType<DateTimeOffset?>(
            serialize: (DateTimeOffset? dto) => dto.HasValue ? new BsonValue(dto.Value.UtcDateTime) : BsonValue.Null,
            deserialize: (BsonValue bson) => bson.IsNull ? null : new DateTimeOffset(bson.AsDateTime));

        // Configure nullable DateTimeOffset serialization
        mapper.RegisterType<DateTimeOffset?>(
            serialize: (DateTimeOffset? dto) => dto.HasValue ? new BsonValue(dto.Value.UtcDateTime) : BsonValue.Null,
            deserialize: (BsonValue bson) => 
            {
                if (bson.IsNull) return null;
                var dt = bson.AsDateTime;
                var utcDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                return new DateTimeOffset(utcDt).ToLocalTime();
            });
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_database.IsValueCreated)
        {
            _database.Value.Dispose();
        }

        _disposed = true;
    }
}
