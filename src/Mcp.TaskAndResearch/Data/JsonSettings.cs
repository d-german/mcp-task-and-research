using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcp.TaskAndResearch.Data;

internal static class JsonSettings
{
    public static JsonSerializerOptions Default { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        options.Converters.Add(new ImmutableArrayJsonConverterFactory());
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new TaskStatusJsonConverter());

        return options;
    }
}
