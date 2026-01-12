using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;

namespace Mcp.TaskAndResearch.Server;

internal static class ServerHost
{
    private const string UiEnabledEnvVar = "TASK_MANAGER_UI";
    private const string UiPortEnvVar = "TASK_MANAGER_UI_PORT";
    private const string UiAutoOpenEnvVar = "TASK_MANAGER_UI_AUTO_OPEN";
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
        // Get the directory where the application is located for proper content root
        // AppContext.BaseDirectory works correctly for global .NET tools (unlike Assembly.Location)
        var assemblyDir = AppContext.BaseDirectory;
        
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = assemblyDir,
            WebRootPath = Path.Combine(assemblyDir, "wwwroot")
        });
        
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
        
        // Find an available port starting from the configured port
        var preferredPort = GetUiPort();
        var actualPort = FindAvailablePort(preferredPort);
        
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(actualPort);
        });
        
        // Enable static web assets from Razor class libraries (like MudBlazor)
        builder.WebHost.UseStaticWebAssets();
        
        var app = builder.Build();
        
        // Log the UI URL
        var uiUrl = $"http://localhost:{actualPort}";
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("TaskManagerUI");
        logger.LogInformation("Task Manager UI available at: {Url}", uiUrl);
        if (actualPort != preferredPort)
        {
            logger.LogWarning("Port {PreferredPort} was in use, using {ActualPort} instead", preferredPort, actualPort);
        }
        
        // Auto-open browser if configured
        if (IsAutoOpenEnabled())
        {
            _ = Task.Run(() => OpenBrowser(uiUrl), CancellationToken.None);
        }
        
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

    private static int FindAvailablePort(int startPort, int maxAttempts = 10)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            var port = startPort + i;
            if (IsPortAvailable(port))
            {
                return port;
            }
        }
        
        return startPort; // Let it fail with clear error if none available
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return false;
        }
    }

    private static bool IsAutoOpenEnabled()
    {
        var envValue = Environment.GetEnvironmentVariable(UiAutoOpenEnvVar);
        return string.Equals(envValue, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            // Ignore errors opening browser
        }
    }
}
