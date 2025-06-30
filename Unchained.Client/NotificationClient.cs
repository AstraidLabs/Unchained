using Microsoft.AspNetCore.SignalR.Client;
using Unchained.Client.Models.Events;

namespace Unchained.Client;

/// <summary>
/// Lightweight SignalR client used to communicate with the server's NotificationHub.
/// </summary>
public class NotificationClient : IAsyncDisposable
{
    private readonly HubConnection _connection;

    /// <summary>
    /// Event raised when a chat message is received from the server.
    /// </summary>
    public event Action<string, string>? MessageReceived;

    /// <summary>
    /// Raised when the server broadcasts a successful login event.
    /// </summary>
    public event Action<UserLoggedInEvent>? UserLoggedIn;

    /// <summary>
    /// Raised when a user logs out.
    /// </summary>
    public event Action<UserLoggedOutEvent>? UserLoggedOut;

    /// <summary>
    /// Raised when tokens have been refreshed for a session.
    /// </summary>
    public event Action<TokensRefreshedEvent>? TokensRefreshed;

    /// <summary>
    /// Raised after an FFmpeg recording job finishes.
    /// </summary>
    public event Action<FfmpegJobCompletedEvent>? FfmpegJobCompleted;

    /// <summary>
    /// Raised when a background work item starts processing.
    /// </summary>
    public event Action<WorkItemStartedEvent>? WorkItemStarted;

    /// <summary>
    /// Raised when a background work item completes.
    /// </summary>
    public event Action<WorkItemCompletedEvent>? WorkItemCompleted;

    /// <summary>
    /// Raised whenever a background service reports a health status change.
    /// </summary>
    public event Action<ServiceHealthChangedEvent>? ServiceHealthChanged;

    /// <summary>
    /// Raised when another client connects to the hub.
    /// </summary>
    public event Action<ClientConnectedEvent>? ClientConnected;

    /// <summary>
    /// Raised when another client disconnects from the hub.
    /// </summary>
    public event Action<ClientDisconnectedEvent>? ClientDisconnected;

    /// <summary>
    /// Raised when the underlying SignalR connection has been closed.
    /// </summary>
    public event Func<Exception?, Task>? ConnectionClosed;

    /// <summary>
    /// Raised when the underlying SignalR connection has been re-established.
    /// </summary>
    public event Func<string?, Task>? Reconnected;

    public NotificationClient(string baseUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl.TrimEnd('/')}/hubs/notifications")
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += error => ConnectionClosed?.Invoke(error) ?? Task.CompletedTask;
        _connection.Reconnected += connectionId => Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;

        _connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            MessageReceived?.Invoke(user, message);
        });

        _connection.On<UserLoggedInEvent>("UserLoggedIn", evt =>
        {
            UserLoggedIn?.Invoke(evt);
        });

        _connection.On<UserLoggedOutEvent>("UserLoggedOut", evt =>
        {
            UserLoggedOut?.Invoke(evt);
        });

        _connection.On<TokensRefreshedEvent>("TokensRefreshed", evt =>
        {
            TokensRefreshed?.Invoke(evt);
        });

        _connection.On<FfmpegJobCompletedEvent>("FfmpegJobCompleted", evt =>
        {
            FfmpegJobCompleted?.Invoke(evt);
        });

        _connection.On<WorkItemStartedEvent>("WorkItemStarted", evt =>
        {
            WorkItemStarted?.Invoke(evt);
        });

        _connection.On<WorkItemCompletedEvent>("WorkItemCompleted", evt =>
        {
            WorkItemCompleted?.Invoke(evt);
        });

        _connection.On<ServiceHealthChangedEvent>("ServiceHealthChanged", evt =>
        {
            ServiceHealthChanged?.Invoke(evt);
        });

        _connection.On<ClientConnectedEvent>("ClientConnected", evt =>
        {
            ClientConnected?.Invoke(evt);
        });

        _connection.On<ClientDisconnectedEvent>("ClientDisconnected", evt =>
        {
            ClientDisconnected?.Invoke(evt);
        });
    }

    public async Task StartAsync() => await _connection.StartAsync();

    public async Task StopAsync() => await _connection.StopAsync();

    public async Task SendMessageAsync(string user, string message)
    {
        await _connection.SendAsync("SendMessage", user, message);
    }

    public async Task<IReadOnlyCollection<string>> GetConnectionsAsync()
    {
        return await _connection.InvokeAsync<List<string>>("GetConnections");
    }

    public async Task<bool> IsConnectedAsync(string connectionId)
    {
        return await _connection.InvokeAsync<bool>("IsConnected", connectionId);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

