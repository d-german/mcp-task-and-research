using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Mcp.TaskAndResearch.UI.Components.Shared;

public partial class AppErrorBoundary : ErrorBoundary
{
    [Inject]
    private ILogger<AppErrorBoundary> Logger { get; set; } = default!;

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Unhandled error caught by AppErrorBoundary: {Message}", exception.Message);
        return base.OnErrorAsync(exception);
    }
}
