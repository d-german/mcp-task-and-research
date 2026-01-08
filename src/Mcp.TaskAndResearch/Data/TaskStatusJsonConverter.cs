using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcp.TaskAndResearch.Data;

internal sealed class TaskStatusJsonConverter : JsonConverter<TaskStatus>
{
    public override TaskStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "pending" => TaskStatus.Pending,
            "in_progress" => TaskStatus.InProgress,
            "inprogress" => TaskStatus.InProgress,
            "completed" => TaskStatus.Completed,
            "blocked" => TaskStatus.Blocked,
            _ => throw new JsonException($"Unsupported task status value '{value}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, TaskStatus value, JsonSerializerOptions options)
    {
        var serialized = value switch
        {
            TaskStatus.Pending => "pending",
            TaskStatus.InProgress => "in_progress",
            TaskStatus.Completed => "completed",
            TaskStatus.Blocked => "blocked",
            _ => "pending"
        };

        writer.WriteStringValue(serialized);
    }
}
