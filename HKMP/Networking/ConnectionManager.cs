using System;
using Hkmp.Logging;
using Hkmp.Networking.Packet.Update;
using Org.BouncyCastle.Tls;

namespace Hkmp.Networking;

/// <summary>
/// Class that manages sending packets while establishing connection to a server.
/// </summary>
internal class ConnectionManager {
    /// <summary>
    /// The maximum size that a slice can be.
    /// </summary>
    public const int MaxSliceSize = 1024;

    public const int MaxSlicesPerChunk = 256;

    public const int MaxChunkSize = MaxSliceSize * MaxSlicesPerChunk;
}

internal class ConnectionManager<TOutgoing, TPacketId> : ConnectionManager
    where TOutgoing : UpdatePacket<TPacketId>, new() 
    where TPacketId : Enum 
{
    
    private readonly DtlsTransport _dtlsTransport;

    protected readonly object Lock = new();
    protected TOutgoing CurrentConnectionPacket;

    public ConnectionManager(DtlsTransport dtlsTransport) {
        _dtlsTransport = dtlsTransport;

        CurrentConnectionPacket = new TOutgoing();
    }

    public void SendPacket() {
        if (_dtlsTransport == null) {
            Logger.Error("DTLS transport is null, cannot send connection packet");
            return;
        }

        var packet = new Packet.Packet();
        TOutgoing connectionPacket;

        lock (Lock) {
            try {
                CurrentConnectionPacket.CreatePacket(packet);
            } catch (Exception e) {
                Logger.Error($"An error occurred while trying to create packet:\n{e}");
                return;
            }

            connectionPacket = CurrentConnectionPacket;
            CurrentConnectionPacket = new TOutgoing();
        }
        
        
    }
}
