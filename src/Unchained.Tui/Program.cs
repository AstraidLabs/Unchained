using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unchained.Tui.Api;
using Unchained.Tui.Common;
using Unchained.Tui.SignalR;
using Unchained.Tui.Ui;
using Terminal.Gui;

namespace Unchained.Tui;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables(prefix: "UNCHAINED_TUI_");
                try
                {
                    config.AddUserSecrets<HostBuilderMarker>(optional: true);
                }
                catch
                {
                    // ignore missing user secrets
                }
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<AppState>();
                services.AddSingleton<NotificationClient>();
                services.AddSingleton<MainWindow>();
                services.AddHttpClient<UnchainedApiClient>((sp, client) =>
                    {
                        var state = sp.GetRequiredService<AppState>().Options;
                        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, state.Http.TimeoutSeconds));
                        var normalized = AppState.NormalizeBaseUrl(state.BaseUrl);
                        if (Uri.TryCreate(normalized, UriKind.Absolute, out var baseUri))
                        {
                            client.BaseAddress = baseUri;
                        }
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.All,
                        UseCookies = true,
                        CookieContainer = new CookieContainer()
                    });
            });

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IConfiguration>()
            .GetSection("Unchained")
            .Get<UnchainedOptions>() ?? new UnchainedOptions();
        host.Services.GetRequiredService<AppState>().Load(options);

        Application.Init();
        var top = Application.Top;
        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        top.Add(mainWindow);

        Application.Run();
        Application.Shutdown();

        await host.StopAsync();
        return 0;
    }
}

internal sealed class HostBuilderMarker
{
}
