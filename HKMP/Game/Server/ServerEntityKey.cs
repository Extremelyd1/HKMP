using System;

namespace Hkmp.Game.Server; 

/// <summary>
/// A key class that uniquely identifies an entity.
/// </summary>
internal class ServerEntityKey : IEquatable<ServerEntityKey> {
    /// <summary>
    /// The scene that the entity is in.
    /// </summary>
    public string Scene { get; }
    /// <summary>
    /// The ID of the entity.
    /// </summary>
    public byte EntityId { get; }

    public ServerEntityKey(string scene, byte entityId) {
        Scene = scene;
        EntityId = entityId;
    }

    public override bool Equals(object obj) {
        if (obj == null || GetType() != obj.GetType()) {
            return false;
        }

        return Equals((ServerEntityKey) obj);
    }

    public bool Equals(ServerEntityKey other) {
        if (other == null) {
            return false;
        }
        
        return Scene == other.Scene && EntityId == other.EntityId;
    }

    public override int GetHashCode() {
        unchecked {
            var hashCode = Scene != null ? Scene.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ EntityId.GetHashCode();
            return hashCode;
        }
    }
}
