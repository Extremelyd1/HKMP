using System.Collections.Generic;
using HKMP.Game.Client.Entity;

namespace HKMP.Networking.Packet.Custom.Update {
    public class EntityUpdate {
        
        public EntityType EntityType { get; set; }
        
        public byte Id { get; set; }
        
        public HashSet<EntityUpdateType> UpdateTypes { get; }
        
        public byte StateIndex { get; set; }

        public List<byte> FsmVariables { get; }

        public EntityUpdate() {
            UpdateTypes = new HashSet<EntityUpdateType>();
            FsmVariables = new List<byte>();
        }
    }

    public enum EntityUpdateType {
        State = 1,
        Variables = 2,
        
        // Represents the number of values in the enum
        Count = 2
    }
}