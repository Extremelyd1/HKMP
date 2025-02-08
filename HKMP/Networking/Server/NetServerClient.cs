using System.Collections.Concurrent;
using System.Net;
using Hkmp.Networking.Chunk;
using Hkmp.Networking.Packet;
using Org.BouncyCastle.Tls;

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
    /// The chunk sender instance for sending large amounts of data.
    /// </summary>
    public ServerChunkSender ChunkSender { get; }
    /// <summary>
    /// The chunk receiver instance for receiving large amounts of data.
    /// </summary>
    public ServerChunkReceiver ChunkReceiver { get; }
    
    /// <summary>
    /// The connection manager for the client.
    /// </summary>
    public ServerConnectionManager ConnectionManager { get; }

    /// <summary>
    /// The endpoint of the client.
    /// </summary>
    public readonly IPEndPoint EndPoint;

    /// <summary>
    /// Construct the client with the given DTLS transport and endpoint.
    /// </summary>
    /// <param name="dtlsTransport">The underlying DTLS transport.</param>
    /// <param name="packetManager">The packet manager used on the server.</param>
    /// <param name="endPoint">The endpoint.</param>
    public NetServerClient(DtlsTransport dtlsTransport, PacketManager packetManager, IPEndPoint endPoint) {
        EndPoint = endPoint;

        Id = GetId();
        UpdateManager = new ServerUpdateManager {
            DtlsTransport = dtlsTransport
        };
        ChunkSender = new ServerChunkSender(UpdateManager);
        ChunkReceiver = new ServerChunkReceiver(UpdateManager);
        ConnectionManager = new ServerConnectionManager(packetManager, ChunkSender, ChunkReceiver, Id);
    }

    /// <summary>
    /// Disconnect the client from the server.
    /// </summary>
    public void Disconnect() {
        UsedIds.TryRemove(Id, out _);

        UpdateManager.StopUpdates();
        ChunkSender.Stop();
        ConnectionManager.StopAcceptingConnection();
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

        UsedIds[newId] = 0;
        return newId;
    }
}
