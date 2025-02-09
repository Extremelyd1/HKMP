using System;
using System.Timers;
using Hkmp.Logging;
using Hkmp.Networking.Chunk;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Connection;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Networking.Server;

internal class ServerConnectionManager : ConnectionManager {
    private readonly ServerChunkSender _chunkSender;
    private readonly ServerChunkReceiver _chunkReceiver;

    private readonly ushort _clientId;

    private readonly Timer _timeoutTimer;

    public event Action<ushort, ClientInfo, ServerInfo> ConnectionRequestEvent;
    public event Action ConnectionTimeoutEvent;

    public ServerConnectionManager(
        PacketManager packetManager,
        ServerChunkSender chunkSender,
        ServerChunkReceiver chunkReceiver,
        ushort clientId
    ) : base(packetManager) {
        _chunkSender = chunkSender;
        _chunkReceiver = chunkReceiver;

        _clientId = clientId;

        _timeoutTimer = new Timer {
            Interval = TimeoutMillis,
            AutoReset = false
        };
        _timeoutTimer.Elapsed += (_, _) => ConnectionTimeoutEvent?.Invoke();

        packetManager.RegisterServerConnectionPacketHandler<ClientInfo>(
            ServerConnectionPacketId.ClientInfo,
            OnClientInfoReceived
        );
        _chunkReceiver.ChunkReceivedEvent += OnChunkReceived;
    }

    public void StartAcceptingConnection() {
        Logger.Debug("StartAcceptingConnection");
        
        _timeoutTimer.Start();
    }

    public void StopAcceptingConnection() {
        Logger.Debug("StopAcceptingConnection");
        
        _timeoutTimer.Stop();
    }

    public void FinishConnection(Action callback) {
        _chunkSender.FinishSendingData(callback);
    }

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
