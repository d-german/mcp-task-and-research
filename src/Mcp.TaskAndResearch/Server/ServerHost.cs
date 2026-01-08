using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mcp.TaskAndResearch.Server;

internal static class ServerHost
{
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        using IHost host = BuildHost(args);
        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IHost BuildHost(string[] args)
    {
        var settings = new HostApplicationBuilderSettings
        {
            Args = args
        };
        var builder = Host.CreateEmptyApplicationBuilder(settings);

        LoggingConfiguration.Configure(builder.Logging);
        ServerServices.Configure(builder.Services);

        return builder.Build();
    }
}
