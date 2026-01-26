namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Immutable result record for import operations.
/// Using record type ensures immutability and value-based equality.
/// </summary>
internal sealed record ImportResult(
    bool Success,
    int TasksImported,
    int SnapshotsImported,
    string? Error = null)
{
    /// <summary>
    /// Factory method for successful import with no data.
    /// </summary>
    public static ImportResult Empty { get; } = new(
        Success: true,
        TasksImported: 0,
        SnapshotsImported: 0,
        Error: null);

    /// <summary>
    /// Factory method for failed import.
    /// </summary>
    public static ImportResult Failure(string error) => new(
        Success: false,
        TasksImported: 0,
        SnapshotsImported: 0,
        Error: error);
}

/// <summary>
/// Immutable result record for export operations.
/// Using record type ensures immutability and value-based equality.
/// </summary>
internal sealed record ExportResult(
    bool Success,
    string? TasksFilePath = null,
    string? MemoryDirectory = null,
    string? Error = null)
{
    /// <summary>
    /// Factory method for failed export.
    /// </summary>
    public static ExportResult Failure(string error) => new(
        Success: false,
        TasksFilePath: null,
        MemoryDirectory: null,
        Error: error);
}

/// <summary>
/// Immutable status record for migration state.
/// Provides information about legacy files without modifying them.
/// </summary>
internal sealed record MigrationStatus(
    bool LegacyFilesExist,
    bool HasTasksJson,
    bool HasMemorySnapshots,
    int LegacyTaskCount,
    int LegacySnapshotCount)
{
    /// <summary>
    /// Factory method for when no legacy files exist.
    /// </summary>
    public static MigrationStatus NoLegacyFiles { get; } = new(
        LegacyFilesExist: false,
        HasTasksJson: false,
        HasMemorySnapshots: false,
        LegacyTaskCount: 0,
        LegacySnapshotCount: 0);
}
