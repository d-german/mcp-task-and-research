namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Service interface for import/export operations between JSON and LiteDB storage systems.
/// Following SOLID principles (Interface Segregation and Dependency Inversion).
/// </summary>
internal interface IMigrationService
{
    /// <summary>
    /// Import tasks from legacy JSON files into the LiteDB database.
    /// </summary>
    /// <param name="sourcePath">Optional path to JSON files. If null, uses default DATA_DIR location.</param>
    /// <returns>Result indicating success, counts of imported items, or error message.</returns>
    Task<ImportResult> ImportFromJsonAsync(string? sourcePath = null);

    /// <summary>
    /// Export current database tasks to JSON format.
    /// </summary>
    /// <param name="destinationPath">Optional destination path. If null, uses default DATA_DIR location.</param>
    /// <returns>Result indicating success, file paths, or error message.</returns>
    Task<ExportResult> ExportToJsonAsync(string? destinationPath = null);

    /// <summary>
    /// Check if legacy files exist and their state.
    /// </summary>
    /// <returns>Status information about legacy files.</returns>
    Task<MigrationStatus> GetMigrationStatusAsync();
}
