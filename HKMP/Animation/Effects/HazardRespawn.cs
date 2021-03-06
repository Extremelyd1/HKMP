using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class HazardRespawn : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // We only have to make the player visible again
            playerObject.SetActive(true);
            
            // TODO: perhaps implement the sprite flash
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}