using System;
using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Networking.Packet.Data {
    public class LoginResponse : IPacketData {
        public bool IsReliable => true;
        
        public bool DropReliableDataIfNewerExists => true;
        
        public LoginResponseStatus LoginResponseStatus { get; set; }
        
        public List<AddonData> AddonData { get; }
        
        public byte[] AddonOrder { get; set; }

        public LoginResponse() {
            AddonData = new List<AddonData>();
        }
        
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
            
                    AddonData.Add(new AddonData {
                        Identifier = id,
                        Version = version
                    });
                }
            }
        }
    }
    
    public enum LoginResponseStatus {
        // When the request has been approved and connection is a success
        Success = 0,
        // When the user is not white-listed
        NotWhiteListed,
        // When there is a mismatch between the addons of the server and the client
        InvalidAddons,
        // When the username is already in use
        InvalidUsername
    }
}