namespace Unchained.Services.Connections;

/// <summary>
/// Simple registry for tracking active SignalR connections.
/// </summary>
public interface IConnectionRegistry
{
    void Add(string connectionId);
    void Remove(string connectionId);
    IReadOnlyCollection<string> GetConnections();
    bool IsConnected(string connectionId);
}
