using System;
using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Networking.Packet.Data {
    public class LoginRequest : IPacketData {
        public bool IsReliable => true;
        
        public bool DropReliableDataIfNewerExists => true;
        
        public string Username { get; set; }
        
        public List<AddonData> AddonData { get; }

        public LoginRequest() {
            AddonData = new List<AddonData>();
        }
        
        public void WriteData(IPacket packet) {
            packet.Write(Username);
            
            var addonDataLength = (byte) System.Math.Min(byte.MaxValue, AddonData.Count);

            packet.Write(addonDataLength);

            for (var i = 0; i < addonDataLength; i++) {
                packet.Write(AddonData[i].Identifier);
                packet.Write(AddonData[i].Version);
            }
        }

        public void ReadData(IPacket packet) {
            Username = packet.ReadString();

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

    public class AddonData {
        public string Identifier { get; set; }
        public string Version { get; set; }
    }
}