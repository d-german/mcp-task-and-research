using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;

namespace Mcp.TaskAndResearch.Server;

internal static class ServerHost
{
    private const string UiEnabledEnvVar = "TASK_MANAGER_UI";
    private const string UiPortEnvVar = "TASK_MANAGER_UI_PORT";
    private const int DefaultUiPort = 9998;

    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var enableUi = IsUiEnabled();
        
        if (enableUi)
        {
            await RunWithBlazorAsync(args, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await RunWithoutBlazorAsync(args, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task RunWithBlazorAsync(string[] args, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure logging
        LoggingConfiguration.Configure(builder.Logging);
        
        // Configure core MCP services
        ServerServices.Configure(builder.Services);
        
        // Configure Blazor Server
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddMudServices();
        
        // Register UI services
        builder.Services.AddScoped<UI.Services.NotificationService>();
        builder.Services.AddSingleton<UI.Services.JsonStringLocalizer>();
        builder.Services.AddSingleton(typeof(Microsoft.Extensions.Localization.IStringLocalizer<>), 
            typeof(UI.Services.JsonStringLocalizer<>));
        
        // Configure Kestrel port
        var port = GetUiPort();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(port);
        });
        
        // Enable static web assets from Razor class libraries (like MudBlazor)
        builder.WebHost.UseStaticWebAssets();
        
        var app = builder.Build();
        
        // Configure middleware for Blazor
        app.UseStaticFiles();
        app.UseAntiforgery();
        
        app.MapRazorComponents<UI.App>()
            .AddInteractiveServerRenderMode();
        
        await app.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunWithoutBlazorAsync(string[] args, CancellationToken cancellationToken)
    {
        var settings = new HostApplicationBuilderSettings
        {
            Args = args
        };
        var builder = Host.CreateEmptyApplicationBuilder(settings);
        
        LoggingConfiguration.Configure(builder.Logging);
        ServerServices.Configure(builder.Services);
        
        using var host = builder.Build();
        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsUiEnabled()
    {
        var envValue = Environment.GetEnvironmentVariable(UiEnabledEnvVar);
        
        // Default to false if not set (UI is opt-in)
        if (string.IsNullOrEmpty(envValue))
        {
            return false;
        }
        
        // Explicitly check for "true" to enable
        return string.Equals(envValue, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetUiPort()
    {
        var portValue = Environment.GetEnvironmentVariable(UiPortEnvVar);
        
        if (int.TryParse(portValue, out var port) && port > 0 && port < 65536)
        {
            return port;
        }
        
        return DefaultUiPort;
    }
}
