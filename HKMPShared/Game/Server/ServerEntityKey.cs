using System;

namespace Hkmp.Game.Server {
    /**
     * A key class that uniquely identifies an entity
     */
    public class ServerEntityKey : IEquatable<ServerEntityKey> {
        public string Scene { get; }
        public byte EntityType { get; }
        public byte EntityId { get; }

        public ServerEntityKey(string scene, byte entityType, byte entityId) {
            Scene = scene;
            EntityType = entityType;
            EntityId = entityId;
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            return Equals((ServerEntityKey) obj);
        }

        public bool Equals(ServerEntityKey other) {
            return Scene == other.Scene && EntityType == other.EntityType && EntityId == other.EntityId;
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (Scene != null ? Scene.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ EntityType.GetHashCode();
                hashCode = (hashCode * 397) ^ EntityId.GetHashCode();
                return hashCode;
            }
        }
    }
}