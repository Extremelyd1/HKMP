using System.Collections.Generic;
using UnityEngine;

namespace HKMP.Networking.Packet.Custom.Update {
    public class PlayerUpdate {
        // ID: ushort - 2 bytes
        public ushort Id { get; set; }

        public HashSet<PlayerUpdateType> UpdateTypes { get; }

        // Position: 3x float - 3x4 = 12 bytes
        public Vector3 Position { get; set; } = Vector3.zero;
        
        // Scale: 3x float - 3x4 = 12 bytes
        public Vector3 Scale { get; set; } = Vector3.zero;
        
        // Map position: 3x float - 3x4 = 12 bytes
        public Vector3 MapPosition { get; set; } = Vector3.zero;

        public List<AnimationInfo> AnimationInfos { get; }

        public PlayerUpdate() {
            UpdateTypes = new HashSet<PlayerUpdateType>();
            AnimationInfos = new List<AnimationInfo>();
        }
    }
    
    public enum PlayerUpdateType {
        Position = 1,
        Scale = 2,
        MapPosition = 4,
        Animation = 8,
        
        // Represents the number of values in the enum
        Count = 4
    }

    public class AnimationInfo {
        public ushort ClipId { get; set; }
        public byte Frame { get; set; }
        public bool[] EffectInfo { get; set; }
    }
}