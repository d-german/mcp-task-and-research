# LiteDB Migration Plan

## Overview

Migration from JSON file-based storage to LiteDB embedded database. This eliminates race conditions through built-in ACID transactions, improves performance via page caching/indexing, and removes ~150 lines of manual locking/retry code.

**Branch:** `feature/litedb-migration`

---

## Phase 1: Setup

### Task 1: Add LiteDB NuGet Package

**WHAT**: Add the LiteDB NuGet package reference to the project file.

**WHY**: LiteDB is the embedded NoSQL database that will replace JSON file storage. It provides built-in ACID transactions, thread-safety, and eliminates the need for manual locking code. The package is ~450KB and has no external dependencies.

**Files:**
- `src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj` (MODIFY)

**Verification:**
- [ ] LiteDB package reference added to csproj
- [ ] Project builds successfully with both net8.0 and net9.0
- [ ] dotnet restore completes without errors

---

### Task 2: Create LiteDB Database Provider

**WHAT**: Create a new `LiteDbProvider` class that manages the LiteDB database instance lifecycle, providing a singleton `LiteDatabase` instance.

**WHY**: Following Single Responsibility Principle, database connection management should be separate from data operations. This provider will handle database path resolution, connection string configuration (exclusive mode for performance), and proper disposal.

**Files:**
- `src/Mcp.TaskAndResearch/Data/LiteDbProvider.cs` (CREATE)
- `src/Mcp.TaskAndResearch/Data/DataPathProvider.cs` (REFERENCE)

**Dependencies:** Task 1

**Verification:**
- [ ] LiteDbProvider class created with IDisposable pattern
- [ ] Uses DataPathProvider to determine database file location
- [ ] Connection string uses Exclusive mode for performance
- [ ] Implements interface for testability
- [ ] Database file created in correct location on first access

---

### Task 3: Update TaskItem Model for LiteDB

**WHAT**: Add LiteDB attributes to TaskItem and related models. Add [BsonId] to TaskItem.Id and configure ImmutableArray serialization.

**WHY**: LiteDB uses BSON for storage and has different serialization requirements than System.Text.Json. The [BsonId] attribute designates the primary key. ImmutableArray<T> requires custom BSON mapping.

**Files:**
- `src/Mcp.TaskAndResearch/Data/TaskModels.cs` (MODIFY)

**Dependencies:** Task 1

**Verification:**
- [ ] [BsonId] attribute on TaskItem.Id property
- [ ] BsonMapper configured for ImmutableArray<T> serialization
- [ ] TaskStatus enum serializes correctly
- [ ] DateTimeOffset properties serialize/deserialize accurately

---

## Phase 2: Abstractions

### Task 4: Create ITaskRepository Interface

**WHAT**: Create a new `ITaskRepository` interface that defines all task data operations (CRUD + query methods).

**WHY**: Dependency Inversion Principle - consumers should depend on abstractions, not concrete implementations. This enables swapping between JSON and LiteDB implementations during migration and facilitates unit testing.

**Files:**
- `src/Mcp.TaskAndResearch/Data/ITaskRepository.cs` (CREATE)
- `src/Mcp.TaskAndResearch/Data/ITaskReader.cs` (REFERENCE)

**Verification:**
- [ ] Interface includes all methods from current TaskStore
- [ ] Inherits from or replaces ITaskReader
- [ ] Uses Task<T> for async operations
- [ ] OnTaskChanged event included for UI notifications

---

### Task 5: Implement LiteDbTaskRepository

**WHAT**: Create `LiteDbTaskRepository` class implementing `ITaskRepository`. This replaces TaskStore's JSON file operations with LiteDB collection operations.

**WHY**: This is the core of the migration. LiteDB's built-in ACID transactions and thread-safety eliminate the need for SemaphoreSlim locking, retry logic, and atomic file operations.

**Files:**
- `src/Mcp.TaskAndResearch/Data/LiteDbTaskRepository.cs` (CREATE)
- `src/Mcp.TaskAndResearch/Data/TaskStore.cs` (REFERENCE)

**Dependencies:** Tasks 2, 3, 4

**Verification:**
- [ ] Implements all ITaskRepository methods
- [ ] No SemaphoreSlim or manual locking code
- [ ] No retry logic needed
- [ ] Uses LiteCollection<TaskItem> for operations
- [ ] EnsureIndex on Id and Name fields
- [ ] OnTaskChanged event fires correctly

---

### Task 6: Create IMemoryRepository Interface

**WHAT**: Create `IMemoryRepository` interface abstracting the MemoryStore operations (snapshot read/write for task history).

**WHY**: Same Dependency Inversion rationale. MemoryStore currently writes JSON snapshots of completed tasks. With LiteDB, these can be stored in a separate collection.

**Files:**
- `src/Mcp.TaskAndResearch/Data/IMemoryRepository.cs` (CREATE)
- `src/Mcp.TaskAndResearch/Data/MemoryStore.cs` (REFERENCE)

**Verification:**
- [ ] Interface includes WriteSnapshotAsync and ReadAllSnapshotsAsync methods
- [ ] Uses appropriate return types

---

### Task 7: Implement LiteDbMemoryRepository

**WHAT**: Create `LiteDbMemoryRepository` implementing `IMemoryRepository`. Stores task snapshots in a LiteDB collection instead of individual JSON files.

**WHY**: Consolidates all data into a single database file. Snapshots become queryable documents rather than separate files.

**Files:**
- `src/Mcp.TaskAndResearch/Data/LiteDbMemoryRepository.cs` (CREATE)
- `src/Mcp.TaskAndResearch/Data/MemoryStore.cs` (REFERENCE)

**Dependencies:** Tasks 2, 6

**Verification:**
- [ ] Implements IMemoryRepository
- [ ] Uses separate 'snapshots' collection
- [ ] Stores timestamp with each snapshot
- [ ] Snapshots queryable by date

---

## Phase 3: Integration

### Task 8: Update DI Registration in ServerServices

**WHAT**: Update `ServerServices.Configure()` to register LiteDB provider and repositories.

**WHY**: Dependency Injection wiring must be updated for the new implementations.

**Files:**
- `src/Mcp.TaskAndResearch/Server/ServerServices.cs` (MODIFY)

**Dependencies:** Tasks 5, 7

**Verification:**
- [ ] LiteDbProvider registered as singleton
- [ ] ITaskRepository mapped to LiteDbTaskRepository
- [ ] IMemoryRepository mapped to LiteDbMemoryRepository
- [ ] Application starts successfully

---

### Task 9: Update TaskTools to Use ITaskRepository

**WHAT**: Update all TaskTools methods to depend on `ITaskRepository` instead of concrete `TaskStore`.

**WHY**: Following Dependency Inversion, tools should depend on the interface abstraction.

**Files:**
- `src/Mcp.TaskAndResearch/Tools/Task/TaskTools.cs` (MODIFY)

**Dependencies:** Tasks 4, 5

**Verification:**
- [ ] All TaskStore parameters changed to ITaskRepository
- [ ] No direct TaskStore references remain
- [ ] All tools compile and function correctly

---

### Task 10: Update TaskServices to Use ITaskRepository

**WHAT**: Update `TaskBatchService`, `TaskWorkflowService`, and other service classes.

**WHY**: Services should depend on abstractions to remain decoupled from storage implementation.

**Files:**
- `src/Mcp.TaskAndResearch/Tools/Task/TaskServices.cs` (MODIFY)

**Dependencies:** Task 4

**Verification:**
- [ ] TaskBatchService uses ITaskRepository
- [ ] TaskWorkflowService uses ITaskRepository
- [ ] All services compile correctly

---

### Task 11: Update TaskSearchService

**WHAT**: Update `TaskSearchService` to depend on repository interfaces.

**WHY**: Search service should depend on repository abstractions for consistency.

**Files:**
- `src/Mcp.TaskAndResearch/Data/TaskSearchService.cs` (MODIFY)

**Dependencies:** Tasks 4, 6

**Verification:**
- [ ] Uses ITaskRepository instead of TaskStore
- [ ] Uses IMemoryRepository instead of MemoryStore
- [ ] Search functionality works correctly

---

### Task 12: Update UI Components

**WHAT**: Update Blazor UI components to use repository interfaces.

**WHY**: UI components should depend on abstractions for consistency.

**Files:**
- `src/Mcp.TaskAndResearch/UI/Components/Dialogs/TaskDetailDialog.razor.cs` (MODIFY)
- `src/Mcp.TaskAndResearch/UI/Components/Pages/HistoryView.razor.cs` (MODIFY)
- `src/Mcp.TaskAndResearch/UI/Components/Pages/TasksPage.razor.cs` (REFERENCE)

**Dependencies:** Tasks 4, 6

**Verification:**
- [ ] All UI components use interfaces
- [ ] UI displays data correctly
- [ ] Real-time updates via OnTaskChanged work

---

## Phase 4: Testing & Cleanup

### Task 13: Update Unit Tests

**WHAT**: Update tests to work with the new repository interfaces. Use LiteDB in-memory mode for testing.

**WHY**: Tests must validate the new implementation. LiteDB supports in-memory mode (connection string: ':memory:').

**Files:**
- `tests/Mcp.TaskAndResearch.Tests/Data/TaskStorageTests.cs` (MODIFY)
- `tests/Mcp.TaskAndResearch.Tests/Tools/TaskToolsTests.cs` (MODIFY)

**Dependencies:** Tasks 5, 7

**Verification:**
- [ ] All existing tests pass
- [ ] Tests use in-memory LiteDB or mocks
- [ ] No file system dependencies in tests

---

### Task 14: Add Data Migration Utility

**WHAT**: Create a utility to migrate existing JSON task data to LiteDB format.

**WHY**: Existing users have tasks stored in JSON files. We need a seamless upgrade path.

**Files:**
- `src/Mcp.TaskAndResearch/Data/DataMigration.cs` (CREATE)

**Dependencies:** Tasks 5, 7

**Verification:**
- [ ] Detects existing JSON data
- [ ] Reads all tasks from JSON
- [ ] Inserts into LiteDB
- [ ] Creates backup of JSON before migration
- [ ] Only runs once

---

### Task 15: Remove Legacy JSON Code

**WHAT**: Remove or deprecate the old JSON-based TaskStore and MemoryStore.

**WHY**: Once migration is complete, legacy code becomes dead weight.

**Files:**
- `src/Mcp.TaskAndResearch/Data/TaskStore.cs` (MODIFY/REMOVE)
- `src/Mcp.TaskAndResearch/Data/MemoryStore.cs` (MODIFY/REMOVE)

**Dependencies:** Tasks 8, 13, 14

**Verification:**
- [ ] Old classes removed or marked obsolete
- [ ] Build succeeds with no warnings
- [ ] All tests still pass

---

### Task 16: Integration Testing

**WHAT**: Perform end-to-end integration testing.

**WHY**: Unit tests validate individual components, but integration testing validates the complete system.

**Files:**
- `tests/Mcp.TaskAndResearch.E2ETests/` (REFERENCE)

**Dependencies:** Tasks 8, 12, 14

**Verification:**
- [ ] Create task via split_tasks works
- [ ] List tasks returns correct data
- [ ] Data persists across server restart
- [ ] UI displays all data correctly
- [ ] No race conditions under rapid operations
- [ ] Migration from JSON works

---

## Phase 5: Release

### Task 17: Documentation Update

**WHAT**: Update README.md to reflect the LiteDB storage change.

**WHY**: Users need to know where their data is stored and how to back it up.

**Files:**
- `README.md` (MODIFY)

**Dependencies:** Task 16

**Verification:**
- [ ] Database file location documented
- [ ] Backup procedure explained
- [ ] Migration from JSON mentioned

---

### Task 18: Version Bump and Release

**WHAT**: Bump version number, create git tag, and prepare release notes.

**WHY**: This is a significant internal change that improves reliability and performance.

**Files:**
- `src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj` (MODIFY)

**Dependencies:** Task 17

**Verification:**
- [ ] Version incremented (minor version for feature)
- [ ] All tests pass
- [ ] Package can be installed globally
- [ ] Release notes written

---

## Code Quality Checklist (Apply to All Code Tasks)

```
📋 CODE QUALITY CHECKLIST:
□ Single Responsibility: Does this class/method do one thing?
□ Open/Closed: Is this open for extension, closed for modification?
□ Liskov Substitution: Are derived classes substitutable for base classes?
□ Interface Segregation: Are interfaces specific rather than general-purpose?
□ Dependency Inversion: Are dependencies injected, not created?
□ Cyclomatic Complexity: Is complexity around 5 or less?
□ Pure Functions: Are there unnecessary side effects?
□ Immutability: Can data structures be immutable?
□ Static Methods: Should any methods be static (no instance state)?
□ Railway-Oriented: Use Result<T> with Bind/Map/Tap instead of try-catch where appropriate
```
