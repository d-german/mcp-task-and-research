using System.Reflection;
using Mcp.ProcessThought.Config;
using Mcp.ProcessThought.Prompts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.2";

if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
{
    Console.WriteLine(version);
    return;
}

if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "-?"))
{
    PrintHelp(version);
    return;
}

var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { Args = args });

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<PathResolver>();
builder.Services.AddSingleton<PromptTemplateLoader>();
builder.Services.AddSingleton<ProcessThoughtPromptBuilder>();

builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "McpProcessThought",
            Version = version
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

using var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);

static void PrintHelp(string version)
{
    Console.WriteLine($$"""
    MCP Process Thought Server v{{version}}
    ======================================
    A Model Context Protocol (MCP) server exposing one concise tool for
    structured, evidence-based reasoning.

    USAGE:
      This tool is an MCP server designed to be invoked by MCP clients (e.g., VS Code, Claude Desktop).
      It is not meant to be run directly from the command line.

    VS CODE SETUP:
      Add to your .vscode/mcp.json (create if it doesn't exist):

      {
        "servers": {
          "process-thought": {
            "type": "stdio",
            "command": "mcp-process-thought"
          }
        }
      }

    CLAUDE DESKTOP SETUP:
      Add to claude_desktop_config.json:

      {
        "mcpServers": {
          "process-thought": {
            "command": "mcp-process-thought"
          }
        }
      }

    ENVIRONMENT VARIABLES:
      DATA_DIR                        Optional. Directory containing prompt template overrides
                                      (for example, <DATA_DIR>/en/processThought/index.md).
      TEMPLATES_USE                   Optional. Template set name (default: "en").
      MCP_PROMPT_PROCESS_THOUGHT      Optional. Fully replace the process_thought prompt.
      MCP_PROMPT_PROCESS_THOUGHT_APPEND
                                      Optional. Append text to the process_thought prompt.

    MORE INFO:
      GitHub:  https://github.com/d-german/mcp-task-and-research
      NuGet:   https://www.nuget.org/packages/Mcp.ProcessThought
    """);
}
