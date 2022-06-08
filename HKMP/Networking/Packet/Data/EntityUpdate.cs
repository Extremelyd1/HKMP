using System;
using System.Collections.Generic;
using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data {
    /// <summary>
    /// Packet data for an entity update.
    /// </summary>
    internal class EntityUpdate : IPacketData {
        /// <inheritdoc />
        public bool IsReliable => false;

        /// <inheritdoc />
        public bool DropReliableDataIfNewerExists => false;

        /// <summary>
        /// The ID of the entity.
        /// </summary>
        public byte Id { get; set; }

        /// <summary>
        /// A set containing the types of updates contained in this packet.
        /// </summary>
        public HashSet<EntityUpdateType> UpdateTypes { get; }

        /// <summary>
        /// The position of the entity.
        /// </summary>
        public Vector2 Position { get; set; }
        
        /// <summary>
        /// The boolean representation of the scale of the entity.
        /// </summary>
        public bool Scale { get; set; }
        
        /// <summary>
        /// The ID of the animation of the entity.
        /// </summary>
        public byte AnimationId { get; set; }
        
        public List<byte> RawData { get; }

        /// <summary>
        /// Construct the entity update data.
        /// </summary>
        public EntityUpdate() {
            UpdateTypes = new HashSet<EntityUpdateType>();

            RawData = new List<byte>();
        }

        /// <inheritdoc />
        public void WriteData(IPacket packet) {
            packet.Write(Id);

            // Construct the byte flag representing update types
            byte updateTypeFlag = 0;
            // Keep track of value of current bit
            byte currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(EntityUpdateType)).Length; i++) {
                // Cast the current index of the loop to a PlayerUpdateType and check if it is
                // contained in the update type list, if so, we add the current bit to the flag
                if (UpdateTypes.Contains((EntityUpdateType)i)) {
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
            
            if (UpdateTypes.Contains(EntityUpdateType.Scale)) {
                packet.Write(Scale);
            }

            if (UpdateTypes.Contains(EntityUpdateType.Animation)) {
                packet.Write(AnimationId);
            }

            if (UpdateTypes.Contains(EntityUpdateType.Raw)) {
                if (RawData.Count > byte.MaxValue) {
                    Logger.Get().Error(this, "Length of raw data exceeded max value of byte");
                }
                
                var length = (byte)System.Math.Min(RawData.Count, byte.MaxValue);

                packet.Write(length);
                for (var i = 0; i < length; i++) {
                    packet.Write(RawData[i]);
                }
            }
        }

        /// <inheritdoc />
        public void ReadData(IPacket packet) {
            Id = packet.ReadByte();

            // Read the byte flag representing update types and reconstruct it
            var updateTypeFlag = packet.ReadByte();
            // Keep track of value of current bit
            var currentTypeValue = 1;

            for (var i = 0; i < Enum.GetNames(typeof(EntityUpdateType)).Length; i++) {
                // If this bit was set in our flag, we add the type to the list
                if ((updateTypeFlag & currentTypeValue) != 0) {
                    UpdateTypes.Add((EntityUpdateType)i);
                }

                // Increase the value of current bit
                currentTypeValue *= 2;
            }

            // Based on the update types, we read the corresponding values
            if (UpdateTypes.Contains(EntityUpdateType.Position)) {
                Position = packet.ReadVector2();
            }
            
            if (UpdateTypes.Contains(EntityUpdateType.Scale)) {
                Scale = packet.ReadBool();
            }

            if (UpdateTypes.Contains(EntityUpdateType.Animation)) {
                AnimationId = packet.ReadByte();
            }

            if (UpdateTypes.Contains(EntityUpdateType.Raw)) {
                var length = packet.ReadByte();

                for (var i = 0; i < length; i++) {
                    RawData.Add(packet.ReadByte());
                }
            }
        }
    }

    /// <summary>
    /// Enumeration of entity update types.
    /// </summary>
    internal enum EntityUpdateType {
        Position = 0,
        Scale,
        Animation,
        Raw
    }
}