using HKMP.Networking.Packet.Custom;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class Recoil : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Get the player effects object to put new effects in
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            
            // If the SD Trail from a crystal dash was playing, we stop it
            Object.Destroy(playerEffects.FindGameObjectInChildren("SD Trail"));
            
            // Obtain the gameObject containing damage effects
            var damageEffect = HeroController.instance.gameObject.FindGameObjectInChildren("Damage Effect");

            // Instantiate a hit crack effect
            var hitCrack = Object.Instantiate(
                damageEffect.FindGameObjectInChildren("Hit Crack"),
                playerEffects.transform
            );
            hitCrack.SetActive(true);
            
            // Instantiate a object responsible for particle effects
            var hitPt1 = Object.Instantiate(
                damageEffect.FindGameObjectInChildren("Hit Pt 1"),
                playerEffects.transform
            );
            hitPt1.SetActive(true);
            // Play the particle effect
            hitPt1.GetComponent<ParticleSystem>().Play();
            
            // Instantiate a object responsible for particle effects
            var hitPt2 = Object.Instantiate(
                damageEffect.FindGameObjectInChildren("Hit Pt 2"),
                playerEffects.transform
            );
            hitPt2.SetActive(true);
            // Play the particle effect
            hitPt2.GetComponent<ParticleSystem>().Play();
            
            // Destroy all objects after 1 second
            Object.Destroy(hitCrack, 1);
            Object.Destroy(hitPt1, 1);
            Object.Destroy(hitPt2, 1);
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}