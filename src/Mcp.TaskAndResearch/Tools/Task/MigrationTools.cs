using System.ComponentModel;
using Mcp.TaskAndResearch.Data;
using ModelContextProtocol.Server;

namespace Mcp.TaskAndResearch.Tools.Task;

[McpServerToolType]
internal static class MigrationTools
{
    [McpServerTool(Name = "import_legacy_tasks")]
    [Description("Import tasks and history from legacy JSON files (tasks.json and memory/*.json) into the database. Use this to migrate data from older versions.")]
    public static async Task<string> ImportLegacyTasks(IMigrationService migrationService)
    {
        var result = await migrationService.ImportFromJsonAsync().ConfigureAwait(false);

        if (!result.Success)
        {
            return $"❌ Import failed: {result.Error}";
        }

        if (result.TasksImported == 0 && result.SnapshotsImported == 0)
        {
            return "ℹ️ No legacy files found to import.";
        }

        return $"✅ Successfully imported {result.TasksImported} task(s) and {result.SnapshotsImported} snapshot(s) from legacy JSON files.";
    }

    [McpServerTool(Name = "export_tasks_to_json")]
    [Description("Export all tasks and history from the database to JSON files. Creates tasks.json and memory snapshot files in the specified or default location.")]
    public static async Task<string> ExportTasksToJson(
        IMigrationService migrationService,
        [Description("Optional custom path for export, defaults to DATA_DIR.")] string? destinationPath = null)
    {
        var result = await migrationService.ExportToJsonAsync(destinationPath).ConfigureAwait(false);

        if (!result.Success)
        {
            return $"❌ Export failed: {result.Error}";
        }

        return $"✅ Successfully exported tasks to JSON files:\n" +
               $"  - Tasks: {result.TasksFilePath}\n" +
               $"  - Memory: {result.MemoryDirectory}";
    }
}
