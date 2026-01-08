namespace Mcp.TaskAndResearch.Tools.Research;

internal sealed record ResearchModeRequest
{
    public required string Topic { get; init; }
    public string PreviousState { get; init; } = string.Empty;
    public required string CurrentState { get; init; }
    public required string NextSteps { get; init; }
}
