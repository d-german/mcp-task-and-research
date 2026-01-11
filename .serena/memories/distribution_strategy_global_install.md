# Distribution Strategy: Global Install for MCP Server and Task Viewer

## Current State

The project currently requires users to clone the repository to access both:
1. **MCP Server** (.NET) - Configured in VS Code's `.vscode/mcp.json`
2. **Task Viewer** (Node.js/React) - Located in `tools/task-viewer/`

## Proposed Dual Distribution Strategy

Support BOTH installation methods to serve different user needs:

### Option A: End Users (Global Install - No Repo Clone)

```bash
# One-time setup
dotnet tool install -g Mcp.TaskAndResearch
npm install -g shrimp-task-viewer

# VS Code Configuration (.vscode/mcp.json)
{
  "servers": {
    "task-and-research": {
      "type": "stdio",
      "command": "mcp-task-and-research",  // Global tool command
      "env": {
        "DATA_DIR": ".mcp-tasks"  // Relative path works!
      }
    }
  }
}

# Start task viewer
shrimp-viewer start
# Opens http://127.0.0.1:9998
```

### Option B: Developers (Current Method - Clone Repo)

```bash
git clone <repo>
cd mcp-task-and-research

# MCP: VS Code uses local source via dotnet run
# Viewer: cd tools/task-viewer && npm install && npm start
```

## Implementation Steps

### 1. Configure MCP Server as .NET Global Tool

Modify `src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Exe</OutputType>
    
    <!-- .NET Tool Configuration -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>mcp-task-and-research</ToolCommandName>
    <PackageId>Mcp.TaskAndResearch</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>MCP Task and Research Server for AI assistants</Description>
    <PackageProjectUrl>https://github.com/your-org/mcp-task-and-research</PackageProjectUrl>
    <RepositoryUrl>https://github.com/your-org/mcp-task-and-research</RepositoryUrl>
  </PropertyGroup>
  <!-- ... rest of config ... -->
</Project>
```

**Build and publish:**

```bash
# Local testing
dotnet pack
dotnet tool install -g --add-source ./nupkg Mcp.TaskAndResearch

# Publish to NuGet
dotnet pack -c Release
dotnet nuget push ./bin/Release/Mcp.TaskAndResearch.1.0.0.nupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json
```

### 2. Publish Task Viewer to npm

The task-viewer is already configured with the necessary `bin` entry in `package.json`:

```json
{
  "name": "shrimp-task-viewer",
  "version": "3.0.0",
  "bin": {
    "shrimp-viewer": "cli.js"
  }
}
```

**Preparation:**

1. Ensure `files` array in package.json includes all necessary files:
   ```json
   "files": [
     "dist/**/*",
     "server.js",
     "cli.js",
     "public/**/*"
   ]
   ```

2. Build and publish:
   ```bash
   cd tools/task-viewer
   npm run build
   npm publish --access public
   ```

### 3. Update Documentation

Create installation guide showing both methods:

**Quick Install (Users):**
- Install .NET tool from NuGet
- Install task-viewer from npm
- Configure simple .vscode/mcp.json

**From Source (Developers):**
- Clone repository
- Use existing setup process
- Allows customization and contributions

## Architecture Notes

### Component Independence

The MCP server and task-viewer are **completely decoupled**:

- **MCP Server**: Writes to `DATA_DIR` (resolved via workspace root)
- **Task Viewer**: Reads from paths configured in its UI
- **No shared config** - Each component is independently configured

This means:
- Task-viewer can monitor multiple projects simultaneously
- Each workspace can have its own `.mcp-tasks` directory
- Global install doesn't break separation of concerns

### Data Flow

```
VS Code Copilot
    ↓ spawns
MCP Server (.NET) → writes → .mcp-tasks/tasks.json
                                    ↑
                                    | reads
                            Task Viewer (Node.js)
                            (Port 9998)
```

## Benefits

✅ Users don't need to clone repo  
✅ Developers can still work from source  
✅ Version management via standard package managers  
✅ MCP server and UI version independently  
✅ Mirrors Serena's distribution model (PyPI/uvx)  
✅ Task viewer becomes universal dashboard for all projects

## Testing Checklist

Before publishing:

- [ ] Test .NET tool installation: `dotnet tool install -g --add-source ./nupkg Mcp.TaskAndResearch`
- [ ] Verify tool runs: `mcp-task-and-research --help` (if help command exists)
- [ ] Test npm package: `npm pack` then `npm install -g ./shrimp-task-viewer-3.0.0.tgz`
- [ ] Verify CLI works: `shrimp-viewer start`
- [ ] Test VS Code integration with global tool
- [ ] Verify relative DATA_DIR resolution works
- [ ] Test task viewer can connect to multiple projects
- [ ] Document update/uninstall procedures

## Related Files

- `src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj` - .NET tool config
- `tools/task-viewer/package.json` - npm package config
- `.vscode/mcp.json` - VS Code MCP server configuration
- `src/Mcp.TaskAndResearch/Config/PathResolver.cs` - DATA_DIR resolution logic
- `tools/task-viewer/server.js` - Task viewer project configuration
