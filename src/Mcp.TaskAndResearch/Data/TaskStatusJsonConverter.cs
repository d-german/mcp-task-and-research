using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace Mcp.TaskAndResearch.Data;

internal sealed class TaskStatusJsonConverter : JsonConverter<TaskStatus>
{
    public override TaskStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        var result = Parse(value);
        
        return result.IsSuccess 
            ? result.Value 
            : throw new JsonException(result.Error);
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

    /// <summary>
    /// Parses a string value into a TaskStatus using Result pattern.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>Result containing the parsed TaskStatus or an error message.</returns>
    public static Result<TaskStatus> Parse(string? value)
    {
        return value switch
        {
            "pending" => Result.Success(TaskStatus.Pending),
            "in_progress" => Result.Success(TaskStatus.InProgress),
            "inprogress" => Result.Success(TaskStatus.InProgress),
            "completed" => Result.Success(TaskStatus.Completed),
            "blocked" => Result.Success(TaskStatus.Blocked),
            _ => Result.Failure<TaskStatus>($"Unsupported task status value '{value}'.")
        };
    }

    /// <summary>
    /// Tries to parse a string value into a TaskStatus.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="status">The parsed status if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string? value, out TaskStatus status)
    {
        var result = Parse(value);
        status = result.IsSuccess ? result.Value : default;
        return result.IsSuccess;
    }
}
