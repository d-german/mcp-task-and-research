using Mcp.TaskAndResearch.Server;

if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "-?"))
{
    PrintHelp();
    return;
}

await ServerHost.RunAsync(args);

static void PrintHelp()
{
    Console.WriteLine("""
    MCP Task and Research Server v1.0.6
    ====================================
    A Model Context Protocol (MCP) server for task management and research workflows.

    USAGE:
      This tool is an MCP server designed to be invoked by MCP clients (e.g., VS Code, Claude Desktop).
      It is not meant to be run directly from the command line.

    VS CODE SETUP:
      Add to your .vscode/mcp.json (create if it doesn't exist):

      {
        "mcpServers": {
          "task-manager": {
            "command": "mcp-task-and-research",
            "env": {
              "DATA_DIR": "${workspaceFolder}/.mcp-tasks"
            }
          }
        }
      }

    CLAUDE DESKTOP SETUP:
      Add to claude_desktop_config.json:

      {
        "mcpServers": {
          "task-manager": {
            "command": "mcp-task-and-research",
            "env": {
              "DATA_DIR": "/path/to/your/project/.mcp-tasks"
            }
          }
        }
      }

    ENVIRONMENT VARIABLES:
      DATA_DIR              Path to store tasks (RECOMMENDED: set per-project for isolation)
      TASK_MANAGER_UI       Set to "true" to enable the web dashboard
      TASK_MANAGER_UI_PORT  Web dashboard port (default: 9998)

    IMPORTANT - PROJECT ISOLATION:
      Without DATA_DIR, tasks are stored in a shared global location, which causes
      task confusion when working on multiple projects. Always set DATA_DIR to a
      project-specific path like "${workspaceFolder}/.mcp-tasks".

    MORE INFO:
      GitHub:  https://github.com/d-german/mcp-task-and-research
      NuGet:   https://www.nuget.org/packages/Mcp.TaskAndResearch
    """);
}
