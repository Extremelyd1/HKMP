using System;
using System.Collections.Generic;
using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data {
    public class EntityUpdate : IPacketData {
        public bool IsReliable => false;
        
        public bool DropReliableDataIfNewerExists => false;
        public byte EntityType { get; set; }

        public byte Id { get; set; }

        public HashSet<EntityUpdateType> UpdateTypes { get; }

        public Vector2 Position { get; set; }

        public byte State { get; set; }

        public List<byte> Variables { get; }

        public EntityUpdate() {
            UpdateTypes = new HashSet<EntityUpdateType>();
            Variables = new List<byte>();
        }

        public void WriteData(Packet packet) {
            packet.Write(EntityType);
            packet.Write(Id);

            // Construct the byte flag representing update types
            byte updateTypeFlag = 0;
            // Keep track of value of current bit
            byte currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(EntityUpdateType)).Length; i++) {
                // Cast the current index of the loop to a PlayerUpdateType and check if it is
                // contained in the update type list, if so, we add the current bit to the flag
                if (UpdateTypes.Contains((EntityUpdateType) i)) {
                    updateTypeFlag |= currentTypeValue;
                }

                currentTypeValue *= 2;
            }

            // Write the update type flag
            packet.Write(updateTypeFlag);

            // Conditionally write the state and data fields
            if (UpdateTypes.Contains(EntityUpdateType.Position)) {
                packet.Write(Position);
            }

            if (UpdateTypes.Contains(EntityUpdateType.State)) {
                packet.Write(State);
            }

            if (UpdateTypes.Contains(EntityUpdateType.Variables)) {
                // First write the number of bytes we are writing
                packet.Write((byte) Variables.Count);

                foreach (var b in Variables) {
                    packet.Write(b);
                }
            }
        }

        public void ReadData(Packet packet) {
            EntityType = packet.ReadByte();
            Id = packet.ReadByte();

            // Read the byte flag representing update types and reconstruct it
            var updateTypeFlag = packet.ReadByte();
            // Keep track of value of current bit
            var currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(EntityUpdateType)).Length; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    UpdateTypes.Add((EntityUpdateType) i);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }

            // Based on the update types, we read the corresponding values
            if (UpdateTypes.Contains(EntityUpdateType.Position)) {
                Position = packet.ReadVector2();
            }

            if (UpdateTypes.Contains(EntityUpdateType.State)) {
                State = packet.ReadByte();
            }

            if (UpdateTypes.Contains(EntityUpdateType.Variables)) {
                // We first read how many bytes are in the array
                var numBytes = packet.ReadByte();

                for (var i = 0; i < numBytes; i++) {
                    var readByte = packet.ReadByte();
                    Variables.Add(readByte);
                }
            }
        }
    }

    public enum EntityUpdateType {
        Position = 0,
        State,
        Variables,
    }
}