using System;
using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for the server info data.
/// </summary>
internal class ServerInfo : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => false;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;

    /// <summary>
    /// Whether the connection was accepted.
    /// </summary>
    public bool ConnectionAccepted { get; set; }

    /// <summary>
    /// The message detailing why the connection was rejected if it was.
    /// </summary>
    public string ConnectionRejectedMessage { get; set; }
    
    /// <summary>
    /// List of addon data that the server uses.
    /// </summary>
    public List<AddonData> AddonData { get; set; }

    /// <summary>
    /// The order in which the addons have been assigned IDs.
    /// </summary>
    public byte[] AddonOrder { get; set; }
    
    /// <summary>
    /// The save data currently used on the server.
    /// </summary>
    public CurrentSave CurrentSave { get; set; }

    /// <summary>
    /// List of ID, username pairs for each connected client.
    /// </summary>
    public List<(ushort, string)> ClientInfo { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(ConnectionAccepted);

        if (!ConnectionAccepted) {
            packet.Write(ConnectionRejectedMessage);

            AddonData ??= new List<AddonData>();
            
            var addonDataLength = (byte) System.Math.Min(byte.MaxValue, AddonData.Count);

            packet.Write(addonDataLength);

            for (var i = 0; i < addonDataLength; i++) {
                packet.Write(AddonData[i].Identifier);
                packet.Write(AddonData[i].Version);
            }

            return;
        }
        
        packet.Write((byte) AddonOrder.Length);

        foreach (var addonOrderByte in AddonOrder) {
            packet.Write(addonOrderByte);
        }
        
        CurrentSave.WriteData(packet);
        
        packet.Write((ushort) ClientInfo.Count);

        foreach (var (id, username) in ClientInfo) {
            packet.Write(id);
            packet.Write(username);
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        ConnectionAccepted = packet.ReadBool();

        if (!ConnectionAccepted) {
            ConnectionRejectedMessage = packet.ReadString();
            
            var addonDataLength = packet.ReadByte();

            AddonData = new List<AddonData>();

            for (var i = 0; i < addonDataLength; i++) {
                var id = packet.ReadString();
                var version = packet.ReadString();

                if (id.Length > Addon.MaxNameLength || version.Length > Addon.MaxVersionLength) {
                    throw new ArgumentException("Identifier or version of addon exceeds max length");
                }

                AddonData.Add(new AddonData(id, version));
            }

            return;
        }
        
        var addonOrderLength = packet.ReadByte();
        AddonOrder = new byte[addonOrderLength];

        for (var i = 0; i < addonOrderLength; i++) {
            AddonOrder[i] = packet.ReadByte();
        }
        
        CurrentSave.ReadData(packet);
        
        var length = packet.ReadUShort();

        for (var i = 0; i < length; i++) {
            ClientInfo.Add((
                packet.ReadUShort(),
                packet.ReadString()
            ));
        }
    }
}
