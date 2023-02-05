using System;
using System.Collections.Generic;
using Hkmp.Math;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for an entity update.
/// </summary>
internal class EntityUpdate : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => false;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;

    /// <summary>
    /// The type of the entity.
    /// </summary>
    public byte EntityType { get; set; }

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
    /// The state of the entity.
    /// </summary>
    public byte State { get; set; }

    /// <summary>
    /// A list of variables for the entity.
    /// </summary>
    public List<byte> Variables { get; }

    /// <summary>
    /// Construct the entity update data.
    /// </summary>
    public EntityUpdate() {
        UpdateTypes = new HashSet<EntityUpdateType>();
        Variables = new List<byte>();
    }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
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

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
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

/// <summary>
/// Enumeration of entity update types.
/// </summary>
internal enum EntityUpdateType {
    Position = 0,
    State,
    Variables,
}
