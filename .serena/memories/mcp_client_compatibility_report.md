# MCP Client Compatibility Report

## Overview
Comprehensive analysis of MCP (Model Context Protocol) support across major AI coding clients, focusing on local configuration override behavior and compatibility with the Mcp.TaskAndResearch stdio server.

## Compatibility Matrix

| Client | Local Override | Config Path | Transports | Compatible with Mcp.TaskAndResearch |
|--------|---------------|-------------|------------|-------------------------------------|
| **VS Code** | ✅ Yes | `.vscode/mcp.json` | stdio, http | ✅ Yes |
| **Claude Desktop** | ❌ No | `~/Library/.../claude_desktop_config.json` (macOS), `%APPDATA%/Claude/claude_desktop_config.json` (Windows) | stdio | ✅ Yes |
| **Claude Code** | ✅ Yes | `.mcp.json` (project root) | stdio, http, sse, ws | ✅ Yes |
| **Claude CLI** | ❓ Unknown | No docs found | Unknown | ❓ Unknown |
| **Gemini CLI** | ✅ Yes | `.gemini/settings.json` | stdio, sse, http | ✅ Yes |
| **Augment Code** | ❌ No (extension-scoped) | Settings Panel / JSON import | stdio, http, sse | ✅ Yes |
| **Codex CLI** | ❌ No | `~/.codex/config.toml` | stdio, http | ✅ Yes |

## Detailed Client Analysis

### VS Code
- **Local Override**: Yes - `.vscode/mcp.json` overrides global user profile settings
- **Config Location**: `.vscode/mcp.json` (workspace) or user profile
- **CLI Support**: `code --add-mcp` can target workspace or global
- **Transports**: stdio (command/args/env), http
- **Source**: code.visualstudio.com/docs/copilot/chat/mcp-servers

### Claude Desktop
- **Local Override**: No - single global config
- **Config Locations**:
  - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
  - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
  - Linux: `~/.config/Claude/claude_desktop_config.json`
- **Transports**: stdio (command/args/env)
- **Source**: github.com/github/github-mcp-server

### Claude Code (Anthropic Agentic CLI)
- **Local Override**: Yes - project `.mcp.json` provides local override
- **Config Location**: `.mcp.json` at project root or via SDK `mcpConfig` option
- **Plugin System**: Also supports `.mcp.json` or `plugin.json` mcpServers field
- **Transports**: stdio, http, sse, websocket
- **Source**: docs.claude.com/en/docs/claude-code/sdk/sdk-mcp, github.com/anthropics/claude-code

### Claude CLI (standalone)
- **Status**: No official MCP configuration documentation found
- **Local Override**: Unknown
- **Compatibility**: Unknown pending official docs

### Gemini CLI
- **Local Override**: Yes - hierarchical precedence
- **Config Hierarchy**:
  1. Project: `.gemini/settings.json` (highest precedence)
  2. User: `~/.gemini/settings.json`
  3. System: `/etc/gemini-cli/settings.json` (lowest precedence)
- **Commands**: `gemini mcp add|list|remove` or `mcpServers` block in settings.json
- **Transports**: stdio (command/args/cwd), sse (url), http (httpUrl)
- **Source**: github.com/philschmid/gemini-samples

### Augment Code
- **Local Override**: No explicit workspace vs global; extension-scoped
- **Config Methods**:
  1. VS Code extension Settings Panel (Easy MCP or manual)
  2. Import from JSON (mcpServers format)
  3. Web app for Code Review (app.augmentcode.com)
- **Transports**: stdio (command/args/env), http, sse, OAuth
- **Source**: docs.augmentcode.com/setup-augment/mcp

### Codex CLI (OpenAI)
- **Local Override**: No - single user config shared by CLI and IDE extension
- **Config Location**: `~/.codex/config.toml` using `[mcp_servers.name]` tables
- **Commands**: `codex mcp add|list|remove`
- **Transports**: stdio (command/args/env), http (url/bearer_token)
- **Source**: developers.openai.com/codex/mcp

## Compatibility Summary

**Mcp.TaskAndResearch Server**:
- Uses stdio command transport
- Fully compatible with: VS Code, Claude Desktop, Claude Code, Gemini CLI, Augment Code, Codex CLI
- Unknown compatibility: Claude CLI (lacks official documentation)

## Example Configuration for Mcp.TaskAndResearch

### VS Code (.vscode/mcp.json)
```json
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "mcp-task-and-research",
      "env": {
        "DATA_DIR": ".mcp-tasks",
        "TASK_MANAGER_UI": "true",
        "TASK_MANAGER_UI_AUTO_OPEN": "true"
      }
    }
  }
}
```

### Gemini CLI (.gemini/settings.json)
```json
{
  "mcpServers": {
    "task-and-research": {
      "command": "mcp-task-and-research",
      "env": {
        "DATA_DIR": ".mcp-tasks"
      }
    }
  }
}
```

### Codex CLI (~/.codex/config.toml)
```toml
[mcp_servers.task-and-research]
command = "mcp-task-and-research"

[mcp_servers.task-and-research.env]
DATA_DIR = ".mcp-tasks"
```

## Key Findings

1. **Local Override Support**: VS Code, Claude Code, and Gemini CLI support project-level config that overrides global settings
2. **Single Global Config**: Claude Desktop, Augment Code (extension-scoped), and Codex CLI use single config locations
3. **Universal stdio Support**: All documented clients support stdio transport, making this MCP server broadly compatible
4. **Documentation Gap**: Claude CLI (standalone) lacks official MCP configuration documentation

## Research Date
January 12, 2026
