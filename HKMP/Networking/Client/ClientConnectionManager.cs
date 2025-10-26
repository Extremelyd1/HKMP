using System;
using System.Collections.Generic;
using Hkmp.Logging;
using Hkmp.Networking.Chunk;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Connection;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Client;

/// <summary>
/// Client-side manager for handling the initial connection to the server.
/// </summary>
internal class ClientConnectionManager : ConnectionManager {
    /// <summary>
    /// The client-side chunk sender used to handle sending chunks.
    /// </summary>
    private readonly ClientChunkSender _chunkSender;
    /// <summary>
    /// The client-side chunk received used to receive chunks.
    /// </summary>
    private readonly ClientChunkReceiver _chunkReceiver;

    /// <summary>
    /// Event that is called when server info is received from the server we are trying to connect to.
    /// </summary>
    public event Action<ServerInfo> ServerInfoReceivedEvent;

    /// <summary>
    /// Construct the connection manager with the given packet manager and chunk sender, and receiver instances.
    /// Will register handlers in the packet manager that relate to the connection.
    /// </summary>
    public ClientConnectionManager(
        PacketManager packetManager,
        ClientChunkSender chunkSender,
        ClientChunkReceiver chunkReceiver
    ) : base(packetManager) {
        _chunkSender = chunkSender;
        _chunkReceiver = chunkReceiver;

        packetManager.RegisterClientConnectionPacketHandler<ServerInfo>(
            ClientConnectionPacketId.ServerInfo,
            OnServerInfoReceived
        );
        _chunkReceiver.ChunkReceivedEvent += OnChunkReceived;
    }

    /// <summary>
    /// Start establishing the connection to the server with the given information.
    /// </summary>
    /// <param name="username">The username of the player.</param>
    /// <param name="authKey">The authentication key of the player.</param>
    /// <param name="addonData">List of addon data that represents the enabled networked addons that the client uses.
    /// </param>
    public void StartConnection(string username, string authKey, List<AddonData> addonData) {
        Logger.Debug("StartConnection");

        // Create a connection packet that will be the entire chunk we will be sending
        var connectionPacket = new ServerConnectionPacket();

        // Set the client info data in the connection packet
        connectionPacket.SetSendingPacketData(ServerConnectionPacketId.ClientInfo, new ClientInfo {
            Username = username,
            AuthKey = authKey,
            AddonData = addonData
        });

        // Create the raw packet from the connection packet
        var packet = new Packet.Packet();
        connectionPacket.CreatePacket(packet);

        // Enqueue the raw packet to be sent using the chunk sender
        _chunkSender.EnqueuePacket(packet);
    }

    /// <summary>
    /// Callback method for when server info is received from the server.
    /// </summary>
    /// <param name="serverInfo">The server info instance received from the server.</param>
    private void OnServerInfoReceived(ServerInfo serverInfo) {
        Logger.Debug($"ServerInfo received, connection accepted: {serverInfo.ConnectionResult}");
        
        ServerInfoReceivedEvent?.Invoke(serverInfo);
    }

    /// <summary>
    /// Callback method for when a new chunk is received from the server.
    /// </summary>
    /// <param name="packet">The raw packet that contains the data from the chunk.</param>
    private void OnChunkReceived(Packet.Packet packet) {
        // Create the connection packet instance and try to read it
        var connectionPacket = new ClientConnectionPacket();
        if (!connectionPacket.ReadPacket(packet)) {
            Logger.Debug("Received malformed connection packet chunk from server");
            return;
        }

        // Let the packet manager handle the connection packet, which will invoke the relevant data handlers
        PacketManager.HandleClientConnectionPacket(connectionPacket);
    }
}
