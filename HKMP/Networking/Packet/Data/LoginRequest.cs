using System;
using System.Collections.Generic;
using Hkmp.Api.Addon;
using Hkmp.Util;

namespace Hkmp.Networking.Packet.Data {
    /// <summary>
    /// Packet data for the login request data.
    /// </summary>
    internal class LoginRequest : IPacketData {
        /// <inheritdoc />
        public bool IsReliable => true;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => true;

        /// <summary>
        /// The username of the client.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The authentication key of the client.
        /// </summary>
        public string AuthKey { get; set; }

        /// <summary>
        /// A list of addon data of the client.
        /// </summary>
        public List<AddonData> AddonData { get; }

        /// <summary>
        /// Construct the login request.
        /// </summary>
        public LoginRequest() {
            AddonData = new List<AddonData>();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet) {
            packet.Write(Username);

            for (var i = 0; i < AuthUtil.AuthKeyLength; i++) {
                packet.Write(StringUtil.CharByteDict[AuthKey[i]]);
            }

            var addonDataLength = (byte)System.Math.Min(byte.MaxValue, AddonData.Count);

            packet.Write(addonDataLength);

            for (var i = 0; i < addonDataLength; i++) {
                packet.Write(AddonData[i].Identifier);
                packet.Write(AddonData[i].Version);
            }
        }

        /// <inheritdoc />
        public void ReadData(IPacket packet) {
            Username = packet.ReadString();

            AuthKey = "";
            for (var i = 0; i < AuthUtil.AuthKeyLength; i++) {
                AuthKey += StringUtil.CharByteDict[packet.ReadByte()];
            }

            var addonDataLength = packet.ReadByte();

            for (var i = 0; i < addonDataLength; i++) {
                var id = packet.ReadString();
                var version = packet.ReadString();

                if (id.Length > Addon.MaxNameLength || version.Length > Addon.MaxVersionLength) {
                    throw new ArgumentException("Identifier or version of addon exceeds max length");
                }

                AddonData.Add(new AddonData {
                    Identifier = id,
                    Version = version
                });
            }
        }
    }

    /// <summary>
    /// Addon data class denoting identifying information about an addon.
    /// </summary>
    internal class AddonData {
        /// <summary>
        /// The identifier of the addon (aka name).
        /// </summary>
        public string Identifier { get; set; }
        /// <summary>
        /// The version of the addon.
        /// </summary>
        public string Version { get; set; }
    }
}