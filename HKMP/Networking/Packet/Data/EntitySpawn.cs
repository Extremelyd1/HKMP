using Hkmp.Game.Client.Entity;

namespace Hkmp.Networking.Packet.Data; 

/// <summary>
/// Packet data for an entity spawn. Either from entering a scene or from spawning while in a scene.
/// </summary>
internal class EntitySpawn : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;

    /// <summary>
    /// The ID of the spawned entity.
    /// </summary>
    public byte Id { get; set; }
    
    /// <summary>
    /// The type of the entity that spawned the new entity.
    /// </summary>
    public EntityType SpawningType { get; set; }
    
    /// <summary>
    /// The type of the entity that was spawned.
    /// </summary>
    public EntityType SpawnedType { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(Id);
        packet.Write((ushort) SpawningType);
        packet.Write((ushort) SpawnedType);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        Id = packet.ReadByte();
        SpawningType = (EntityType) packet.ReadUShort();
        SpawnedType = (EntityType) packet.ReadUShort();
    }
}
