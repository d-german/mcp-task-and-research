using Mcp.TaskAndResearch.Config;
using Mcp.TaskAndResearch.Prompts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mcp.TaskAndResearch.Tests.Prompts;

public sealed class PromptTemplateTests
{
    [Fact]
    public void Render_ReplacesParameters()
    {
        var template = "Hello {name}.";
        var result = PromptTemplateRenderer.Render(template, new Dictionary<string, object?>
        {
            ["name"] = "Ada"
        });

        Assert.Equal("Hello Ada.", result);
    }

    [Fact]
    public void LoadTemplate_ReadsBuiltInTemplate()
    {
        var resolver = new PathResolver(new WorkspaceRootStore(), new ConfigReader(), NullLogger<PathResolver>.Instance);
        var loader = new PromptTemplateLoader(resolver);

        var content = loader.LoadTemplate("tests/basic.md");

        Assert.Equal("Hello {name}.", content.Trim());
    }
}
