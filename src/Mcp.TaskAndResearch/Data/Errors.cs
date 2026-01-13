namespace Mcp.TaskAndResearch.Data;

/// <summary>
/// Base interface for domain errors providing consistent error representation.
/// </summary>
public interface IDomainError
{
    /// <summary>
    /// Gets a user-friendly error message.
    /// </summary>
    string Message { get; }
}

/// <summary>
/// Error when a task cannot be found by ID or name.
/// </summary>
/// <param name="TaskId">The ID of the task that was not found.</param>
public sealed record TaskNotFoundError(string TaskId) : IDomainError
{
    public string Message => $"Task not found: {TaskId}";
    public override string ToString() => Message;
}

/// <summary>
/// Error when task validation fails.
/// </summary>
/// <param name="ValidationMessage">Description of the validation failure.</param>
public sealed record TaskValidationError(string ValidationMessage) : IDomainError
{
    public string Message => $"Task validation failed: {ValidationMessage}";
    public override string ToString() => Message;
}

/// <summary>
/// Error when a file operation fails.
/// </summary>
/// <param name="Path">The file path involved in the operation.</param>
/// <param name="Operation">The operation that failed (e.g., "read", "write", "delete").</param>
/// <param name="Details">Additional error details.</param>
public sealed record FileOperationError(string Path, string Operation, string? Details = null) : IDomainError
{
    public string Message => string.IsNullOrEmpty(Details)
        ? $"File {Operation} operation failed for: {Path}"
        : $"File {Operation} operation failed for: {Path}. {Details}";
    public override string ToString() => Message;
}

/// <summary>
/// Error when task dependency resolution fails.
/// </summary>
/// <param name="TaskName">The name of the task with dependency issues.</param>
/// <param name="Reason">The reason for the resolution failure.</param>
public sealed record DependencyResolutionError(string Details) : IDomainError
{
    public string Message => $"Dependency resolution failed: {Details}";
    public override string ToString() => Message;
}

/// <summary>
/// Error when a prompt template cannot be found.
/// </summary>
/// <param name="TemplatePath">The path to the missing template.</param>
/// <param name="CheckedPaths">The paths that were searched.</param>
public sealed record TemplateNotFoundError(string TemplatePath, string[] CheckedPaths) : IDomainError
{
    public string Message => $"Template file not found: '{TemplatePath}'. Checked paths:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", CheckedPaths)}";
    public override string ToString() => Message;
}

/// <summary>
/// Error when task status transition is invalid.
/// </summary>
/// <param name="TaskId">The task ID.</param>
/// <param name="CurrentStatus">The current status of the task.</param>
/// <param name="TargetStatus">The attempted target status.</param>
public sealed record InvalidStatusTransitionError(string TaskId, string CurrentStatus, string TargetStatus) : IDomainError
{
    public string Message => $"Cannot transition task '{TaskId}' from '{CurrentStatus}' to '{TargetStatus}'";
    public override string ToString() => Message;
}

/// <summary>
/// Error when JSON serialization or deserialization fails.
/// </summary>
/// <param name="Operation">The operation that failed ("serialize" or "deserialize").</param>
/// <param name="TypeName">The type being processed.</param>
/// <param name="Details">Additional error details.</param>
public sealed record JsonError(string Operation, string TypeName, string? Details = null) : IDomainError
{
    public string Message => string.IsNullOrEmpty(Details)
        ? $"JSON {Operation} failed for type: {TypeName}"
        : $"JSON {Operation} failed for type: {TypeName}. {Details}";
    public override string ToString() => Message;
}

/// <summary>
/// Generic domain error for cases not covered by specific types.
/// </summary>
/// <param name="ErrorMessage">The error message.</param>

public sealed record DomainError(string ErrorMessage) : IDomainError
{
    public string Message => ErrorMessage;
    public override string ToString() => Message;
}
