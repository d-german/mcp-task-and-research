# Architecture and Code Structure

## Project Structure

```
mcp-task-and-research/
├── src/
│   └── Mcp.TaskAndResearch/          # Main MCP server project
│       ├── Config/                    # Configuration models and path resolution
│       ├── Data/                      # Data models, storage, and JSON handling
│       ├── Prompts/                   # Prompt template system
│       │   └── v1/templates_en/       # English prompt templates (markdown)
│       ├── Server/                    # MCP server host and DI configuration
│       ├── Tools/                     # MCP tool implementations
│       │   ├── Project/               # Project standards tools
│       │   ├── Research/              # Research mode tools
│       │   ├── Task/                  # Task management tools
│       │   └── Thought/               # Thinking/reasoning tools
│       ├── Program.cs                 # Application entry point
│       └── Mcp.TaskAndResearch.csproj
├── tests/
│   └── Mcp.TaskAndResearch.Tests/    # xUnit test project
├── Directory.Build.props              # Shared build properties
├── McpTaskAndResearch.sln             # Visual Studio solution
└── README.md                          # Project documentation
```

## Key Components

### 1. Data Layer (`Data/`)

**Purpose**: Data persistence, JSON serialization, and domain models

Key Classes:
- **`TaskStore`**: Main data access layer for task CRUD operations
  - Handles JSON file read/write with atomicity
  - Uses immutable collections and record updates
  - Implements async/await pattern correctly
  
- **`MemoryStore`**: Manages memory files and task snapshots
  
- **`RulesStore`**: Manages project rules (shrimp-rules.md)
  
- **`TaskModels`**: Domain models
  - `TaskItem` - Main task entity
  - `TaskDocument` - Root document structure
  - `TaskDependency` - Task dependency relationship
  
- **`TaskSearchService`**: Search and query functionality for tasks

- **`DataPathProvider`**: Resolves data directory paths (relative/absolute)

- **`JsonSettings`**: Centralized JSON serialization configuration

### 2. Server Layer (`Server/`)

**Purpose**: MCP server hosting, dependency injection, and initialization

Key Classes:
- **`ServerHost`**: Configures and runs the MCP server host
  - Sets up DI container
  - Registers all tools
  - Configures logging
  
- **`ServerServices`**: DI service registration
  
- **`WorkspaceRootInitializer`**: Determines workspace root via MCP roots protocol
  
- **`LoggingConfiguration`**: Logging setup

- **`McpServerAccessor`**: Access to MCP server instance

- **`ServerMetadata`**: Server version and metadata

### 3. Tools Layer (`Tools/`)

**Purpose**: MCP tool implementations (exposed to AI clients)

#### Task Tools (`Tools/Task/`)
- **`TaskTools`**: Implements all task management tools
  - `PlanTask` - Create structured task lists
  - `AnalyzeTask` - Deep analysis of requirements
  - `ReflectTask` - Critical review of analysis
  - `SplitTasks` - Break down complex tasks
  - `ListTasks` - List tasks by status
  - `QueryTask` - Search tasks
  - `GetTaskDetail` - Get full task details
  - `ExecuteTask` - Get execution guidance
  - `VerifyTask` - Verify completion
  - `UpdateTask` - Update task details
  - `DeleteTask` - Delete tasks
  - `ClearAllTasks` - Clear all incomplete tasks

- **`TaskServices`**: Helper services for task operations

- **`TaskPromptBuilders`**: Constructs prompts for tool responses

#### Research Tools (`Tools/Research/`)
- **`ResearchModeTool`**: Guided research mode for technical topics
- **`ResearchPromptBuilder`**: Prompt construction for research

#### Project Tools (`Tools/Project/`)
- **`ProjectTools`**: Project rules initialization and management

#### Thought Tools (`Tools/Thought/`)
- **`ThoughtTools`**: Structured thinking and reasoning tools

### 4. Prompts Layer (`Prompts/`)

**Purpose**: Template-based prompt generation for tool responses

Key Classes:
- **`PromptTemplateLoader`**: Loads markdown templates from disk
- **`PromptTemplateRenderer`**: Renders templates with data (Handlebars-style)
- **`PromptCustomization`**: Customization of prompt behavior

Structure:
- Templates organized by tool name in `v1/templates_en/`
- Each tool has its own folder with markdown files
- Templates use placeholders like `{{taskName}}`, `{{description}}`

### 5. Config Layer (`Config/`)

**Purpose**: Configuration models and path resolution

Key Classes:
- **`ConfigReader`**: Reads environment variables and configuration
- **`PathResolver`**: Resolves workspace root and data directory paths
  - Supports MCP roots protocol
  - Handles relative vs absolute paths
  - Protected directory detection

## Architectural Patterns

### Dependency Injection
- Uses `Microsoft.Extensions.Hosting` for DI
- All dependencies injected via constructor
- Services registered in `ServerServices.ConfigureServices()`

### Immutability
- Domain models are `record` types
- Collections use `ImmutableArray<T>`
- Updates create new instances via `with` expressions

### Async/Await
- All I/O operations are async
- Consistent use of `.ConfigureAwait(false)`
- Methods named with `Async` suffix

### Separation of Concerns
- Data access isolated in `Data/` layer
- Business logic in `Tools/` layer
- Hosting/infrastructure in `Server/` layer
- Prompt generation in `Prompts/` layer

### Static Helpers
- Utility methods that don't need instance state are `static`
- Examples: `EnsureDirectory`, `FindTaskIndex`, `ToDependencies`

## Data Flow

1. **MCP Client** → MCP Server (ModelContextProtocol)
2. **MCP Server** → Tool (e.g., `TaskTools.CreateTask`)
3. **Tool** → Data Layer (e.g., `TaskStore.CreateAsync`)
4. **Data Layer** → File System (JSON serialization)
5. **Tool** → Prompt Builder (generate response)
6. **Prompt Builder** → Template Renderer (render markdown)
7. **MCP Server** → MCP Client (return result)

## Testing Strategy

- xUnit for unit tests
- Test projects mirror source structure
- Focus on:
  - Data layer operations
  - Dependency resolution
  - Prompt rendering
  - Path resolution logic

## Key Design Decisions

1. **JSON File Storage**: Simple, portable, human-readable persistence
2. **Immutable Collections**: Thread-safety and functional style
3. **Template-based Prompts**: Flexibility and maintainability
4. **Per-Repository Isolation**: Cleaner task context via relative DATA_DIR
5. **MCP Roots Protocol**: Auto-detection of workspace root
6. **TimeProvider Abstraction**: Testability of time-dependent operations
