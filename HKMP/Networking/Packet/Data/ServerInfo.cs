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
    /// The result of the connection, whether it was accepted.
    /// </summary>
    public ServerConnectionResult ConnectionResult { get; set; }

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
    /// The server settings for the server. Packaged as a <see cref="Data.ServerSettingsUpdate"/> to allow serialization
    /// to packet data.
    /// </summary>
    public ServerSettingsUpdate ServerSettingsUpdate { get; set; }

    /// <summary>
    /// Whether full synchronisation is enabled for the server.
    /// </summary>
    public bool FullSynchronisation { get; set; }

    /// <summary>
    /// The save data currently used on the server.
    /// </summary>
    public CurrentSave CurrentSave { get; set; }

    /// <summary>
    /// List of ID, username pairs for each connected client.
    /// </summary>
    public List<(ushort, string)> PlayerInfo { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write((byte) ConnectionResult);

        if (ConnectionResult == ServerConnectionResult.Accepted) {
            packet.Write((byte) AddonOrder.Length);

            foreach (var addonOrderByte in AddonOrder) {
                packet.Write(addonOrderByte);
            }

            ServerSettingsUpdate.WriteData(packet);
            
            packet.Write(FullSynchronisation);

            CurrentSave.WriteData(packet);
        
            packet.Write((ushort) PlayerInfo.Count);

            foreach (var (id, username) in PlayerInfo) {
                packet.Write(id);
                packet.Write(username);
            }

            return;
        }

        if (ConnectionResult == ServerConnectionResult.InvalidAddons) {
            AddonData ??= new List<AddonData>();
            
            var addonDataLength = (byte) System.Math.Min(byte.MaxValue, AddonData.Count);

            packet.Write(addonDataLength);

            for (var i = 0; i < addonDataLength; i++) {
                packet.Write(AddonData[i].Identifier);
                packet.Write(AddonData[i].Version);
            }

            return;
        }

        packet.Write(ConnectionRejectedMessage);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        ConnectionResult = (ServerConnectionResult) packet.ReadByte();

        if (ConnectionResult == ServerConnectionResult.Accepted) {
            var addonOrderLength = packet.ReadByte();
            AddonOrder = new byte[addonOrderLength];

            for (var i = 0; i < addonOrderLength; i++) {
                AddonOrder[i] = packet.ReadByte();
            }

            ServerSettingsUpdate = new ServerSettingsUpdate();
            ServerSettingsUpdate.ReadData(packet);

            FullSynchronisation = packet.ReadBool();

            CurrentSave = new CurrentSave();
            CurrentSave.ReadData(packet);
        
            var length = packet.ReadUShort();

            PlayerInfo = new List<(ushort, string)>();
            for (var i = 0; i < length; i++) {
                PlayerInfo.Add((
                    packet.ReadUShort(),
                    packet.ReadString()
                ));
            }

            return;
        }

        if (ConnectionResult == ServerConnectionResult.InvalidAddons) {
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

        ConnectionRejectedMessage = packet.ReadString();
    }
}
