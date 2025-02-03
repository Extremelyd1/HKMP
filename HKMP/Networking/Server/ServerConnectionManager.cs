using System;
using Hkmp.Logging;
using Hkmp.Networking.Chunk;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Connection;
using Hkmp.Networking.Packet.Data;
using Hkmp.Networking.Packet.Update;

namespace Hkmp.Networking.Server;

internal class ServerConnectionManager : ConnectionManager {
    private readonly ServerChunkSender _chunkSender;
    private readonly ServerChunkReceiver _chunkReceiver;

    private readonly ushort _clientId;

    public event Action<ushort, ClientInfo, ServerInfo> ConnectionRequestEvent;

    public ServerConnectionManager(
        ServerUpdateManager serverUpdateManager,
        PacketManager packetManager,
        ushort clientId
    ) : base(packetManager) {
        _chunkSender = new ServerChunkSender(serverUpdateManager);
        _chunkReceiver = new ServerChunkReceiver(serverUpdateManager);

        _clientId = clientId;
        
        _chunkReceiver.ChunkReceivedEvent += OnChunkReceived;
    }

    public void StartAcceptingConnection() {
        Logger.Debug("StartAcceptingConnection");
        
        PacketManager.RegisterServerConnectionPacketHandler<ClientInfo>(
            ServerConnectionPacketId.ClientInfo,
            OnClientInfoReceived
        );
        
        _chunkSender.Start();
    }

    public void StopAcceptingConnection() {
        Logger.Debug("StopAcceptingConnection");
        
        PacketManager.DeregisterServerConnectionPacketHandler(ServerConnectionPacketId.ClientInfo);
        
        _chunkSender.Stop();
    }

    public void ProcessReceivedData(SliceData data) => _chunkReceiver.ProcessReceivedData(data);
    public void ProcessReceivedData(SliceAckData data) => _chunkSender.ProcessReceivedData(data);

    private void OnClientInfoReceived(ushort id, ClientInfo clientInfo) {
        if (id != _clientId) {
            return;
        }

        var serverInfo = new ServerInfo();

        try {
            ConnectionRequestEvent?.Invoke(_clientId, clientInfo, serverInfo);
        } catch (Exception e) {
            Logger.Error($"Exception occurred while executing the connection request event:\n{e}");
        }

        var connectionPacket = new ClientConnectionPacket();
        connectionPacket.SetSendingPacketData(ClientConnectionPacketId.ServerInfo, serverInfo);

        var packet = new Packet.Packet();
        connectionPacket.CreatePacket(packet);

        _chunkSender.EnqueuePacket(packet);
    }

    private void OnChunkReceived(Packet.Packet packet) {
        var connectionPacket = new ServerConnectionPacket();
        if (!connectionPacket.ReadPacket(packet)) {
            Logger.Debug($"Received malformed connection packet chunk from client: {_clientId}");
            return;
        }

        PacketManager.HandleServerConnectionPacket(_clientId, connectionPacket);
    }
}
