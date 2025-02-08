using System;
using System.Collections.Generic;
using Hkmp.Logging;
using Hkmp.Networking.Chunk;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Connection;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Client;

internal class ClientConnectionManager : ConnectionManager {
    private readonly ClientChunkSender _chunkSender;
    private readonly ClientChunkReceiver _chunkReceiver;

    public event Action<ServerInfo> ServerInfoReceivedEvent;

    public ClientConnectionManager(
        PacketManager packetManager,
        ClientChunkSender chunkSender,
        ClientChunkReceiver chunkReceiver
    ) : base(packetManager) {
        _chunkSender = chunkSender;
        _chunkReceiver = chunkReceiver;
        
        _chunkReceiver.ChunkReceivedEvent += OnChunkReceived;
    }

    public void StartConnection(string username, string authKey, List<AddonData> addonData) {
        Logger.Debug("StartConnection");
        
        PacketManager.RegisterClientConnectionPacketHandler<ServerInfo>(
            ClientConnectionPacketId.ServerInfo,
            OnServerInfoReceived
        );

        SendUserInfo(username, authKey, addonData);
    }

    public void StopConnection() {
        Logger.Debug("StopConnection");
        
        PacketManager.DeregisterServerConnectionPacketHandler(ServerConnectionPacketId.ClientInfo);
    }

    private void SendUserInfo(string username, string authKey, List<AddonData> addonData) {
        var connectionPacket = new ServerConnectionPacket();
        
        connectionPacket.SetSendingPacketData(ServerConnectionPacketId.ClientInfo, new ClientInfo {
            Username = username,
            AuthKey = authKey,
            AddonData = addonData
        });

        var packet = new Packet.Packet();
        connectionPacket.CreatePacket(packet);

        _chunkSender.EnqueuePacket(packet);
    }

    private void OnServerInfoReceived(ServerInfo serverInfo) {
        Logger.Debug($"ServerInfo received, connection accepted: {serverInfo.ConnectionResult}");
        
        ServerInfoReceivedEvent?.Invoke(serverInfo);
    }
    
    private void OnChunkReceived(Packet.Packet packet) {
        var connectionPacket = new ClientConnectionPacket();
        if (!connectionPacket.ReadPacket(packet)) {
            Logger.Debug("Received malformed connection packet chunk from server");
            return;
        }
        
        PacketManager.HandleClientConnectionPacket(connectionPacket);
    }
}
