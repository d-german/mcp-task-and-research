using System.Text.Json;
using Mcp.ProcessThought.Prompts;

namespace Mcp.ProcessThought.Tests;

public sealed class ServerManifestTests
{
    [Fact]
    public void ServerManifest_MatchesPackageAndAssembly()
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, ".mcp", "server.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = manifest.RootElement;
        var package = root.GetProperty("packages")[0];
        var assemblyVersion = typeof(ProcessThoughtPromptBuilder).Assembly.GetName().Version?.ToString(3);

        Assert.Equal("io.github.d-german/process-thought", root.GetProperty("name").GetString());
        Assert.Equal(assemblyVersion, root.GetProperty("version").GetString());
        Assert.Equal("nuget", package.GetProperty("registryType").GetString());
        Assert.Equal("Mcp.ProcessThought", package.GetProperty("identifier").GetString());
        Assert.Equal(assemblyVersion, package.GetProperty("version").GetString());
        Assert.Equal("stdio", package.GetProperty("transport").GetProperty("type").GetString());
    }
}
