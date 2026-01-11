# Project Overview

## Purpose
MCP Task and Research Manager is a Model Context Protocol (MCP) server that provides advanced task management and research capabilities for AI assistants. It enables structured task planning, dependency management, and guided research workflows.

## Tech Stack

### Core Technology
- **.NET 9.0** - Target framework
- **C# (latest version)** - Primary programming language
- **Model Context Protocol** - Communication protocol (ModelContextProtocol v0.5.0-preview.1)

### Key Dependencies
- **Microsoft.Extensions.Hosting (10.0.1)** - Application hosting framework providing DI, configuration, and lifetime management
- **System.Text.Json** - JSON serialization (using immutable collections)
- **ImmutableCollections** - Data structures for thread-safe operations

### Testing
- **xUnit 2.9.2** - Testing framework
- **Microsoft.NET.Test.Sdk 17.12.0** - Test runner
- **coverlet.collector 6.0.2** - Code coverage collection

## Key Features

### Task Management
- Task planning and analysis with dependency tracking
- Task workflow (execute, verify, complete) with progress tracking
- Automatic dependency resolution by name or ID
- Task updates (details, dependencies, related files)
- Task queries (search/filter by status, keywords, complexity)

### Research Mode
- Guided research workflow for technical topics
- State management across research sessions
- Next steps planning

### Project Standards
- Project rules initialization and management for coding standards

### Thinking Tools
- Structured thinking with branching and revision support

## Runtime Requirements
- .NET 9.0 SDK or later
- Cross-platform: Windows, macOS, Linux

## Data Storage
All task data is stored in JSON files:
- **Tasks**: `{DATA_DIR}/tasks.json`
- **Project Rules**: `{DATA_DIR}/shrimp-rules.md`
- **Memory Files**: `{DATA_DIR}/memory/`
- **Task Backups**: `{DATA_DIR}/backups/`

Default DATA_DIR is relative path `.mcp-tasks` in workspace root (per-repository isolation recommended).
