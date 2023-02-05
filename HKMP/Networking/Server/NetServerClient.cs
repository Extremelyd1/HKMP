using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Hkmp.Networking.Server;

/// <summary>
/// A client managed by the server. This is only used for communication from server to client.
/// </summary>
internal class NetServerClient {
    /// <summary>
    /// Concurrent dictionary for the set of IDs that are used. We use a dictionary because there is no
    /// standard implementation for a concurrent set.
    /// </summary>
    private static readonly ConcurrentDictionary<ushort, byte> UsedIds = new ConcurrentDictionary<ushort, byte>();

    /// <summary>
    /// The last ID that was assigned.
    /// </summary>
    private static ushort _lastId;

    /// <summary>
    /// The ID of this client.
    /// </summary>
    public ushort Id { get; }

    /// <summary>
    /// Whether the client is registered.
    /// </summary>
    public bool IsRegistered { get; set; }

    /// <summary>
    /// The update manager for the client.
    /// </summary>
    public ServerUpdateManager UpdateManager { get; }

    /// <summary>
    /// The endpoint of the client.
    /// </summary>
    public readonly IPEndPoint EndPoint;

    /// <summary>
    /// Construct the client with the given UDP socket and endpoint.
    /// </summary>
    /// <param name="udpSocket">The underlying UDP socket.</param>
    /// <param name="endPoint">The endpoint.</param>
    public NetServerClient(Socket udpSocket, IPEndPoint endPoint) {
        // Also store endpoint with TCP address and TCP port
        EndPoint = endPoint;

        Id = GetId();
        UpdateManager = new ServerUpdateManager(udpSocket, EndPoint);
    }

    /// <summary>
    /// Whether this client resides at the given endpoint.
    /// </summary>
    /// <param name="endPoint">The endpoint to test for.</param>
    /// <returns>true if the address and port of the endpoint match the endpoint of the client; otherwise
    /// false.</returns>
    public bool HasAddress(IPEndPoint endPoint) {
        return EndPoint.Address.Equals(endPoint.Address) && EndPoint.Port == endPoint.Port;
    }

    /// <summary>
    /// Disconnect the client from the server.
    /// </summary>
    public void Disconnect() {
        UsedIds.TryRemove(Id, out _);

        UpdateManager.StopUpdates();
    }

    /// <summary>
    /// Get a new ID that is not in use by another client.
    /// </summary>
    /// <returns>An unused ID.</returns>
    private static ushort GetId() {
        ushort newId;
        do {
            newId = _lastId++;
        } while (UsedIds.ContainsKey(newId));

        UsedIds[newId] = default;
        return newId;
    }
}
