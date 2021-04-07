using System;
using System.Collections.Generic;

namespace HKMP.Networking.Packet.Data {
    // TODO: extend this to allow a larger/customizable number of instances in the list
    // It is now limited by the size of a byte
    public class PacketDataCollection<T> : IPacketData where T : IPacketData, new() {
        
        public List<T> DataInstances { get; }

        public PacketDataCollection() {
            DataInstances = new List<T>();
        }
        
        public void WriteData(Packet packet) {
            var length = (byte) Math.Min(byte.MaxValue, DataInstances.Count);
            
            packet.Write(length);

            for (var i = 0; i < length; i++) {
                DataInstances[i].WriteData(packet);
            }
        }

        public void ReadData(Packet packet) {
            var length = packet.ReadByte();

            for (var i = 0; i < length; i++) {
                // Create new instance of generic type
                var instance = new T();

                // Read the packet data into the instance
                instance.ReadData(packet);

                // And add it to our already initialized list
                DataInstances.Add(instance);
            }
        }
    }
}