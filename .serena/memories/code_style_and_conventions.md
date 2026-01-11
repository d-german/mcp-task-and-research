# Code Style and Conventions

## Language Features

### C# Version
- **Latest C# version** enabled via `<LangVersion>latest</LangVersion>`

### Nullable Reference Types
- **Enabled** project-wide via `<Nullable>enable</Nullable>`
- All reference types must explicitly declare nullability
- Use `?` suffix for nullable types
- Use `!` null-forgiving operator sparingly and only when null-safety is guaranteed

### Implicit Usings
- **Enabled** via `<ImplicitUsings>enable</ImplicitUsings>`
- Common namespaces automatically imported

## Design Patterns

### SOLID Principles
All code must strictly follow SOLID principles:
- **Single Responsibility**: Each class has one well-defined purpose
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: Many specific interfaces rather than one general-purpose interface
- **Dependency Inversion**: Depend on abstractions, not concretions

### Functional Programming
- **Favor immutability**: Use `record` types and `ImmutableArray<T>`
- **Pure functions**: Methods without side effects whenever possible
- **Higher-order functions**: Use LINQ extensively
- Use expression-bodied members for simple operations

### Static Methods
**Critical**: Any method that does not access instance state (fields, properties, or other instance methods) MUST be declared as `static`. Benefits:
- Performance improvement
- Clarifies intent
- Better testability and reusability

Examples from codebase:
```csharp
private static void EnsureDirectory(string path)
private static int FindTaskIndex(ImmutableArray<TaskItem> tasks, string taskId)
private static TaskItem ApplyUpdates(TaskItem existing, TaskUpdateRequest request, DateTimeOffset now)
```

## Naming Conventions

### Fields
- Private fields: prefix with underscore `_fieldName`
- Examples: `_pathProvider`, `_jsonOptions`, `_timeProvider`

### Classes and Methods
- PascalCase for public members
- Clear, descriptive names
- Use verb phrases for methods: `CreateAsync`, `GetByIdAsync`, `ApplyUpdates`

### Access Modifiers
- Use `internal sealed class` for implementation classes not intended for extension
- Use explicit access modifiers (prefer explicit `private` over implicit)

## Async Patterns

### Naming
- All async methods must end with `Async` suffix
- Examples: `ReadDocumentAsync`, `WriteDocumentAsync`, `GetAllAsync`

### ConfigureAwait
**Always** use `.ConfigureAwait(false)` on async calls to avoid unnecessary context switching:
```csharp
var document = await ReadDocumentAsync().ConfigureAwait(false);
await WriteDocumentAsync(updatedDocument).ConfigureAwait(false);
```

## Data Structures

### Immutability
- Use `ImmutableArray<T>` for collections
- Use `record` types with `with` expressions for updates
- Example:
```csharp
var updatedDocument = document with
{
    Tasks = document.Tasks.Add(task)
};
```

### Null Handling
- Check for `IsDefaultOrEmpty` on ImmutableArrays
- Use nullable types `?` appropriately
- Use null-coalescing: `timeProvider ?? TimeProvider.System`

## File Organization

### Namespace Structure
- One namespace per file
- Namespace matches folder structure
- Example: `Mcp.TaskAndResearch.Data`, `Mcp.TaskAndResearch.Tools.Task`

### Class Organization (Top to Bottom)
1. Private fields
2. Constructor(s)
3. Public methods
4. Private methods
5. Static helper methods

## Error Handling
- Return `null` or use nullable types for "not found" scenarios
- Return boolean for success/failure operations
- Use result types for operations needing rich result information

## Documentation
- Use XML documentation comments for public APIs
- Document non-obvious behavior
- Keep comments up to date with code changes
