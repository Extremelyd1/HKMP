using System.Collections.Generic;
using Hkmp.Math;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Game.Server {
    /**
     * A class containing all the relevant data managed by the server about an entity.
     */
    public class ServerEntityData {
        // Update types for this entity to keep track of which pieces of data have been updated
        // This avoids sending unnecessary data to players that enter the scene
        public HashSet<EntityUpdateType> UpdateTypes { get; }
        
        public Vector2 LastPosition { get; set; }

        public bool LastScale { get; set; }

        public byte LastAnimationIndex { get; set; }
        
        public byte[] LastAnimationInfo { get; set; }
        
        public byte State { get; set; }

        public ServerEntityData() {
            UpdateTypes = new HashSet<EntityUpdateType>();
        }
    }
}