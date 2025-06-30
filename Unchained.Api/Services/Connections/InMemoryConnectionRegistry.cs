using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Unchained.Services.Connections;

/// <summary>
/// In-memory registry for tracking SignalR connections.
/// </summary>
public class InMemoryConnectionRegistry : IConnectionRegistry
{
    private readonly ConcurrentDictionary<string, byte> _connections = new();

    public void Add(string connectionId) => _connections.TryAdd(connectionId, 0);

    public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);

    public IReadOnlyCollection<string> GetConnections() => _connections.Keys.ToList();

    public bool IsConnected(string connectionId) => _connections.ContainsKey(connectionId);
}
