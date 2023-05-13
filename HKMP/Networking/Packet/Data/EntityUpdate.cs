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
    /// <summary>
    /// The wrap mode of the animation.
    /// </summary>
    public byte AnimationWrapMode { get; set; }
        
    /// <summary>
    /// Whether the entity is active or not.
    /// </summary>
    public bool IsActive { get; set; }
        
    public List<EntityNetworkData> GenericData { get; }

    /// <summary>
    /// Construct the entity update data.
    /// </summary>
    public EntityUpdate() {
        UpdateTypes = new HashSet<EntityUpdateType>();
        GenericData = new List<EntityNetworkData>();
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

        if (UpdateTypes.Contains(EntityUpdateType.Scale)) {
            packet.Write(Scale);
        }

        if (UpdateTypes.Contains(EntityUpdateType.Animation)) {
            packet.Write(AnimationId);
            packet.Write(AnimationWrapMode);
        }

        if (UpdateTypes.Contains(EntityUpdateType.Active)) {
            packet.Write(IsActive);
        }

        if (UpdateTypes.Contains(EntityUpdateType.Data)) {
            if (GenericData.Count > byte.MaxValue) {
                Logger.Error("Length of entity network data instances exceeded max value of byte");
            }
                
            var length = (byte)System.Math.Min(GenericData.Count, byte.MaxValue);

            packet.Write(length);
            for (var i = 0; i < length; i++) {
                GenericData[i].WriteData(packet);
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
                UpdateTypes.Add((EntityUpdateType) i);
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
            AnimationWrapMode = packet.ReadByte();
        }

        if (UpdateTypes.Contains(EntityUpdateType.Active)) {
            IsActive = packet.ReadBool();
        }

        if (UpdateTypes.Contains(EntityUpdateType.Data)) {
            var length = packet.ReadByte();

            for (var i = 0; i < length; i++) {
                var entityNetworkData = new EntityNetworkData();
                entityNetworkData.ReadData(packet);
                    
                GenericData.Add(entityNetworkData);
            }
        }
    }
}

/// <summary>
/// Generic data for a networked entity.
/// </summary>
internal class EntityNetworkData {
    /// <summary>
    /// The type of the data.
    /// </summary>
    public DataType Type { get; set; }
    /// <summary>
    /// Packet instance containing the data for easy reading and writing of data.
    /// </summary>
    public Packet Packet { get; set; }

    public EntityNetworkData() {
        Packet = new Packet();
    }

    /// <summary>
    /// Write the data into the given packet.
    /// </summary>
    /// <param name="packet">The packet to write into.</param>
    public void WriteData(IPacket packet) {
        packet.Write((byte)Type);

        var data = Packet.ToArray();
        
        if (data.Length > byte.MaxValue) {
            Logger.Error("Length of entity network data exceeded max value of byte");
        }
            
        var length = (byte)System.Math.Min(data.Length, byte.MaxValue);

        packet.Write(length);
        for (var i = 0; i < length; i++) {
            packet.Write(data[i]);
        }
    }

    /// <summary>
    /// Read the data from the given packet.
    /// </summary>
    /// <param name="packet">The packet to read from.</param>
    public void ReadData(IPacket packet) {
        Type = (DataType) packet.ReadByte();

        var length = packet.ReadByte();
        var data = new byte[length];
        
        for (var i = 0; i < length; i++) {
            data[i] = packet.ReadByte();
        }

        Packet = new Packet(data);
    }

    /// <summary>
    /// Enum for data types.
    /// </summary>
    public enum DataType : byte {
        Fsm = 0,
        HealthManager,
        Rotation,
        Collider
    }
}

/// <summary>
/// Enumeration of entity update types.
/// </summary>
internal enum EntityUpdateType {
    Position = 0,
    Scale,
    Animation,
    Active,
    Data
}
