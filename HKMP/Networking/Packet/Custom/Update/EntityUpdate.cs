using System.Collections.Generic;
using HKMP.Game.Client.Entity;
using UnityEngine;

namespace HKMP.Networking.Packet.Custom.Update {
    public class EntityUpdate {
        
        public EntityType EntityType { get; set; }
        
        public byte Id { get; set; }
        
        public HashSet<EntityUpdateType> UpdateTypes { get; }
        
        public Vector2 Position { get; set; }
        
        public byte StateIndex { get; set; }

        public List<byte> FsmVariables { get; }

        public EntityUpdate() {
            UpdateTypes = new HashSet<EntityUpdateType>();
            FsmVariables = new List<byte>();
        }
    }

    public enum EntityUpdateType {
        Position = 1,
        State = 2,
        Variables = 4,
        
        // Represents the number of values in the enum
        Count = 3
    }
}