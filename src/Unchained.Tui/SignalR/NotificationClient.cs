using System.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Unchained.Tui.Common;

namespace Unchained.Tui.SignalR;

public class NotificationClient : IAsyncDisposable
{
    private readonly AppState _state;
    private readonly ILogger<NotificationClient> _logger;
    private HubConnection? _connection;
    private readonly CookieContainer _cookies = new();

    public event Action<NotificationEvent>? EventReceived;
    public event Action<NotificationStatus>? StatusChanged;

    public NotificationClient(AppState state, ILogger<NotificationClient> logger)
    {
        _state = state;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_state.Options.SignalR.Enabled)
        {
            return;
        }

        var url = BuildHubUrl();
        _connection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.Cookies = _cookies;
                if (_state.Options.Auth.Mode == AuthMode.ApiKey && !string.IsNullOrWhiteSpace(_state.Options.Auth.ApiKey))
                {
                    var header = string.IsNullOrWhiteSpace(_state.Options.Auth.ApiKeyHeader) ? "X-Api-Key" : _state.Options.Auth.ApiKeyHeader;
                    options.Headers.Add(header, _state.Options.Auth.ApiKey);
                }
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers(_connection);

        _connection.Closed += async ex =>
        {
            _logger.LogWarning(ex, "SignalR connection closed");
            StatusChanged?.Invoke(NotificationStatus.Disconnected);
            await Task.CompletedTask;
        };

        _connection.Reconnected += async id =>
        {
            _logger.LogInformation("SignalR reconnected {ConnectionId}", id);
            StatusChanged?.Invoke(NotificationStatus.Connected);
            await Task.CompletedTask;
        };

        _connection.Reconnecting += async ex =>
        {
            _logger.LogWarning(ex, "SignalR reconnecting");
            StatusChanged?.Invoke(NotificationStatus.Reconnecting);
            await Task.CompletedTask;
        };

        StatusChanged?.Invoke(NotificationStatus.Connecting);
        await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
        StatusChanged?.Invoke(NotificationStatus.Connected);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.StopAsync(cancellationToken).ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
        StatusChanged?.Invoke(NotificationStatus.Disconnected);
    }

    private Uri BuildHubUrl()
    {
        var baseUrl = _state.Options.BaseUrl ?? string.Empty;
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("BaseUrl is invalid for SignalR");
        }

        var path = string.IsNullOrWhiteSpace(_state.Options.SignalR.HubPath)
            ? "/hubs/status"
            : _state.Options.SignalR.HubPath;

        return new Uri(baseUri, path.TrimStart('/'));
    }

    private void RegisterHandlers(HubConnection connection)
    {
        connection.On<string>("notify", message =>
        {
            EventReceived?.Invoke(new NotificationEvent("notify", message, DateTimeOffset.Now));
        });

        connection.On<string, string>("event", (name, payload) =>
        {
            EventReceived?.Invoke(new NotificationEvent(name, payload, DateTimeOffset.Now));
        });

        connection.On<string>("status", payload =>
        {
            EventReceived?.Invoke(new NotificationEvent("status", payload, DateTimeOffset.Now));
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
