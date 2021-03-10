using HKMP.Networking.Packet.Custom;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class DungTrailEnd : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            
            // Try to find and destroy the dung particle if it exists 
            Object.Destroy(playerEffects.FindGameObjectInChildren("Dung Particle"));
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}