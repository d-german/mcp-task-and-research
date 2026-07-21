# Mcp.ProcessThought

A standalone [Model Context Protocol](https://modelcontextprotocol.io) (MCP) server exposing one concise tool, `process_thought`, for structured, evidence-based reasoning. It is packaged as a .NET global tool.

This server was extracted from [Mcp.TaskAndResearch](https://www.nuget.org/packages/Mcp.TaskAndResearch) so the reasoning tool can be installed and run on its own, without the task-management, storage, or web-UI dependencies.

## Install

```bash
dotnet tool install --global Mcp.ProcessThought
```

This installs the `mcp-process-thought` command.

With the .NET 10 SDK, it can also be run without a persistent installation:

```bash
dnx Mcp.ProcessThought@1.0.2 --yes
```

The package includes NuGet's `McpServer` metadata and `.mcp/server.json`, so compatible clients can discover it and generate a `dnx` configuration from the NuGet MCP Server tab.

## The `process_thought` tool

Accepts one step in an iterative reasoning sequence and returns only a short status and next-action cue. The tool does not repeat the submitted thought in its result, because MCP already retains the tool arguments in the conversation. This preserves the reasoning context while avoiding duplicate tokens.

The server is stateless; persistence comes from the MCP conversation transcript. Parameters:

| Parameter                | Type       | Required | Description                                  |
| ------------------------ | ---------- | -------- | -------------------------------------------- |
| `thought`                | string     | yes      | Reasoning, evidence, and conclusions for this step. |
| `thought_number`         | int        | yes      | 1-based step number.                         |
| `total_thoughts`         | int        | yes      | Expected step count; increase when needed.   |
| `stage`                  | string     | yes      | Short phase label, such as Analysis or Decision. |
| `tags`                   | string[]   | no       | Topical labels.                              |
| `axioms_used`            | string[]   | no       | Principles applied.                         |
| `assumptions_challenged` | string[]   | no       | Assumptions challenged.                     |
| `next_thought_needed`    | bool       | no       | True only when another step is needed.       |

## Client setup

VS Code (`.vscode/mcp.json`):

```json
{
  "servers": {
    "process-thought": {
      "type": "stdio",
      "command": "mcp-process-thought"
    }
  }
}
```

Claude Desktop (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "process-thought": {
      "command": "mcp-process-thought"
    }
  }
}
```

## Customization

Prompt output can be tailored with environment variables:

- `DATA_DIR` – directory holding template overrides, for example `<DATA_DIR>/en/processThought/index.md`.
- `TEMPLATES_USE` – template set name (default `en`).
- `MCP_PROMPT_PROCESS_THOUGHT` – fully replace the prompt.
- `MCP_PROMPT_PROCESS_THOUGHT_APPEND` – append text to the prompt.

## License

MIT
