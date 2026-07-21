using Mcp.ProcessThought.Config;
using Mcp.ProcessThought.Prompts;

namespace Mcp.ProcessThought.Tests;

public sealed class ProcessThoughtPromptBuilderTests
{
    [Fact]
    public void Build_DefaultTemplateAvoidsRepeatingSubmittedContent()
    {
        using var dataDirectory = new TempDirectory();
        using var dataDir = new EnvironmentVariableScope("DATA_DIR", dataDirectory.Path);
        var builder = CreateBuilder();

        var result = builder.Build(
            "Unique reasoning that is already present in the MCP tool arguments.",
            1,
            2,
            "Analysis",
            ["correctness"],
            ["avoid duplication"],
            ["echoing adds value"],
            true);

        Assert.Equal(
            "Thought 1/2 accepted. Continue only if useful; adjust total_thoughts as needed.",
            NormalizeNewLines(result));
        Assert.DoesNotContain("Unique reasoning", result);
        Assert.DoesNotContain("correctness", result);
    }

    [Fact]
    public void Build_CompletionReturnsConciseActionCue()
    {
        using var dataDirectory = new TempDirectory();
        using var dataDir = new EnvironmentVariableScope("DATA_DIR", dataDirectory.Path);
        var builder = CreateBuilder();

        var result = builder.Build("Conclusion", 2, 2, "Decision", [], [], [], false);

        Assert.Equal(
            "Thought 2/2 accepted. Reasoning complete; act on the conclusions.",
            NormalizeNewLines(result));
    }

    [Fact]
    public void Build_CustomTemplateSupportsLegacyPlaceholdersWithoutReprocessingInput()
    {
        using var dataDirectory = new TempDirectory();
        using var dataDir = new EnvironmentVariableScope("DATA_DIR", dataDirectory.Path);
        var templateDirectory = Path.Combine(dataDirectory.Path, "en", "processThought");
        Directory.CreateDirectory(templateDirectory);
        File.WriteAllText(
            Path.Combine(templateDirectory, "index.md"),
            "{thought}|{stage}|{tags}|{axioms_used}|{assumptions_challenged}|{metadata}|{nextThoughtNeeded}");
        File.WriteAllText(Path.Combine(templateDirectory, "moreThought.md"), "continue");
        File.WriteAllText(Path.Combine(templateDirectory, "complatedThought.md"), "complete");
        var builder = CreateBuilder();

        var result = builder.Build(
            "Keep {stage} literal",
            1,
            1,
            " Analysis ",
            [" one ", "one", ""],
            ["principle"],
            ["assumption"],
            false);

        Assert.Contains("Keep {stage} literal|Analysis|one|principle|assumption|", result);
        Assert.Contains("**Tags:** one", result);
        Assert.EndsWith("|complete", result);
    }

    [Theory]
    [InlineData("", 1, 1, "Analysis")]
    [InlineData("Thought", 0, 1, "Analysis")]
    [InlineData("Thought", 2, 1, "Analysis")]
    [InlineData("Thought", 1, 1, " ")]
    public void Build_InvalidRequiredInputThrows(string thought, int thoughtNumber, int totalThoughts, string stage)
    {
        using var dataDirectory = new TempDirectory();
        using var dataDir = new EnvironmentVariableScope("DATA_DIR", dataDirectory.Path);
        var builder = CreateBuilder();

        Assert.ThrowsAny<ArgumentException>(() =>
            builder.Build(thought, thoughtNumber, totalThoughts, stage, [], [], [], false));
    }

    private static ProcessThoughtPromptBuilder CreateBuilder()
    {
        return new ProcessThoughtPromptBuilder(new PromptTemplateLoader(new PathResolver()));
    }

    private static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _originalValue;

        public EnvironmentVariableScope(string name, string? value)
        {
            _name = name;
            _originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _originalValue);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"mcp-process-thought-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
