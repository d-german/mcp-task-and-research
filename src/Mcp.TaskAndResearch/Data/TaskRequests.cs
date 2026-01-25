namespace Mcp.TaskAndResearch.Data;

internal sealed record TaskCreateRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? Notes { get; init; }
    public List<string> Dependencies { get; init; } = [];
    public List<RelatedFile> RelatedFiles { get; init; } = [];
    public string? AnalysisResult { get; init; }
    public string? Agent { get; init; }
    public string? ImplementationGuide { get; init; }
    public string? VerificationCriteria { get; init; }
}

public sealed record TaskUpdateRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public TaskStatus? Status { get; init; }
    public List<string>? Dependencies { get; init; }
    public List<RelatedFile>? RelatedFiles { get; init; }
    public string? Summary { get; init; }
    public string? AnalysisResult { get; init; }
    public string? Agent { get; init; }
    public string? ImplementationGuide { get; init; }
    public string? VerificationCriteria { get; init; }
}
