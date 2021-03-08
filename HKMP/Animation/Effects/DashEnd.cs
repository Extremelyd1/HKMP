using HKMP.Networking.Packet.Custom;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class DashEnd : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Enable the player collider again
            playerObject.GetComponent<BoxCollider2D>().enabled = true;
            
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            if (playerEffects == null) {
                return;
            }
            
            var dashParticles = playerEffects.FindGameObjectInChildren("Dash Particles");
            if (dashParticles != null) {
#pragma warning disable 0618
                // Disable emission
                dashParticles.GetComponent<ParticleSystem>().enableEmission = false;
#pragma warning restore 0618
            }

            var shadowDashParticles = playerEffects.FindGameObjectInChildren("Shadow Dash Particles");
            if (shadowDashParticles != null) {
#pragma warning disable 0618
                // Disable emission
                shadowDashParticles.GetComponent<ParticleSystem>().enableEmission = false;
#pragma warning restore 0618
            }
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}