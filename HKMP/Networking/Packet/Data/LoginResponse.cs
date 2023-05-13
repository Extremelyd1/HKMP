using System;
using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for the login response data.
/// </summary>
internal class LoginResponse : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => true;

    /// <summary>
    /// The login response status.
    /// </summary>
    public LoginResponseStatus LoginResponseStatus { get; set; }

    /// <summary>
    /// List of addon data that the server uses.
    /// </summary>
    public List<AddonData> AddonData { get; }

    /// <summary>
    /// The order in which the addons have been assigned IDs.
    /// </summary>
    public byte[] AddonOrder { get; set; }

    /// <summary>
    /// Construct the login response data.
    /// </summary>
    public LoginResponse() {
        AddonData = new List<AddonData>();
    }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write((byte) LoginResponseStatus);

        if (LoginResponseStatus == LoginResponseStatus.Success) {
            packet.Write((byte) AddonOrder.Length);

            foreach (var addonOrderByte in AddonOrder) {
                packet.Write(addonOrderByte);
            }
        } else if (LoginResponseStatus == LoginResponseStatus.InvalidAddons) {
            var addonDataLength = (byte) System.Math.Min(byte.MaxValue, AddonData.Count);

            packet.Write(addonDataLength);

            for (var i = 0; i < addonDataLength; i++) {
                packet.Write(AddonData[i].Identifier);
                packet.Write(AddonData[i].Version);
            }
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        LoginResponseStatus = (LoginResponseStatus) packet.ReadByte();

        if (LoginResponseStatus == LoginResponseStatus.Success) {
            var addonOrderLength = packet.ReadByte();
            AddonOrder = new byte[addonOrderLength];

            for (var i = 0; i < addonOrderLength; i++) {
                AddonOrder[i] = packet.ReadByte();
            }
        } else if (LoginResponseStatus == LoginResponseStatus.InvalidAddons) {
            var addonDataLength = packet.ReadByte();

            for (var i = 0; i < addonDataLength; i++) {
                var id = packet.ReadString();
                var version = packet.ReadString();

                if (id.Length > Addon.MaxNameLength || version.Length > Addon.MaxVersionLength) {
                    throw new ArgumentException("Identifier or version of addon exceeds max length");
                }

                AddonData.Add(new AddonData(id, version));
            }
        }
    }
}

/// <summary>
/// Enumeration of login response statuses.
/// </summary>
internal enum LoginResponseStatus {
    /// <summary>
    /// When the request has been approved and connection is a success.
    /// </summary>
    Success = 0,

    /// <summary>
    /// When the user is not white-listed.
    /// </summary>
    NotWhiteListed,

    /// <summary>
    /// When the user is banned.
    /// </summary>
    Banned,

    /// <summary>
    /// When there is a mismatch between the addons of the server and the client.
    /// </summary>
    InvalidAddons,

    /// <summary>
    /// When the username is already in use.
    /// </summary>
    InvalidUsername
}
