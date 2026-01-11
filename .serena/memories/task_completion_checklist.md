# Task Completion Checklist

When completing a development task, perform the following steps in order:

## 1. Code Quality Checks

### Build Verification
- [ ] **Build succeeds** without errors
  ```powershell
  dotnet build
  ```
- [ ] No compiler warnings introduced
- [ ] Release build succeeds
  ```powershell
  dotnet build -c Release
  ```

### Code Style Verification
- [ ] **SOLID principles** followed
- [ ] **Static methods** used where appropriate (methods not using instance state)
- [ ] **Nullable reference types** handled correctly (no null reference warnings)
- [ ] **Immutability** used (ImmutableArray, record types)
- [ ] **Async/await** uses `.ConfigureAwait(false)`
- [ ] **Naming conventions** followed (_fieldName, PascalCase methods)
- [ ] **Access modifiers** explicit and appropriate

## 2. Testing

### Run Tests
- [ ] **All existing tests pass**
  ```powershell
  dotnet test
  ```
- [ ] **New tests written** for new functionality (if applicable)
- [ ] **Edge cases covered** by tests
- [ ] Test coverage maintained or improved

## 3. Documentation

- [ ] **XML documentation** added for public APIs
- [ ] **Code comments** for non-obvious logic
- [ ] **README updated** if user-facing changes
- [ ] **Memory files updated** if architectural changes

## 4. Git Workflow

### Before Committing
- [ ] Review changes:
  ```powershell
  git status
  git diff
  ```
- [ ] **Remove debug code** and console logging
- [ ] **No commented-out code** left in
- [ ] **Sensitive data** removed (passwords, keys, etc.)

### Commit
- [ ] **Stage appropriate files** only:
  ```powershell
  git add src/Mcp.TaskAndResearch/ChangedFile.cs
  ```
- [ ] **Commit with descriptive message**:
  ```powershell
  git commit -m "Add feature: concise description of changes"
  ```
  - Use present tense: "Add feature" not "Added feature"
  - Be specific: what changed and why
  - Reference issue numbers if applicable

### Push (if applicable)
- [ ] Push to remote:
  ```powershell
  git push
  ```

## 5. Verification

### Runtime Verification
- [ ] Application **runs successfully**:
  ```powershell
  dotnet run --project src/Mcp.TaskAndResearch/Mcp.TaskAndResearch.csproj
  ```
- [ ] **MCP tools respond** correctly
- [ ] **No runtime exceptions** in typical usage
- [ ] **Data persistence** works (if applicable)

### Integration Checks
- [ ] Works with MCP client (Claude Desktop, etc.)
- [ ] Environment variables handled correctly
- [ ] Cross-platform considerations addressed (if applicable)

## 6. Cleanup

- [ ] **Remove temporary files**
- [ ] **Remove debug artifacts**
- [ ] **Clear any test data** created during development
- [ ] Verify `.gitignore` is working correctly

## Common Pitfalls to Avoid

❌ **Don't:**
- Leave debugging code or verbose logging
- Commit commented-out code
- Introduce nullable reference warnings
- Use mutable collections when ImmutableArray is appropriate
- Create methods that should be static but aren't
- Forget `.ConfigureAwait(false)` on awaits
- Leave test failures
- Break existing functionality

✅ **Do:**
- Test edge cases and null handling
- Follow existing code patterns
- Keep SOLID principles in mind
- Write clear, self-documenting code
- Add tests for new functionality
- Update documentation
