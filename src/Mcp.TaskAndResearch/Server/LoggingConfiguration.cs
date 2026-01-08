using Microsoft.Extensions.Logging;

namespace Mcp.TaskAndResearch.Server;

internal static class LoggingConfiguration
{
    public static void Configure(ILoggingBuilder loggingBuilder)
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
    }
}
