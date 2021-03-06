using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class HazardDeath : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Get the effect info
            var hazardWasSpikes = packet.EffectInfo[0];
            var hazardWasAcid = packet.EffectInfo[1];
            
            // Disable the player object so it isn't visible anymore
            playerObject.SetActive(false);

            if (hazardWasSpikes) {
                // Spawn the spike death object relative to the player object
                var spikeDeathPrefab = HeroController.instance.spikeDeathPrefab;
                var spikeDeath = spikeDeathPrefab.Spawn(playerObject.transform.position);
                // Set the spike direction to the default value as in the HeroController
                FSMUtility.SetFloat(spikeDeath.GetComponent<PlayMakerFSM>(), "Spike Direction", 57.29578f);

                // Destroy it after some time
                Object.Destroy(spikeDeath, 2.0f);
            } else if (hazardWasAcid) {
                // Spawn the acid death object relative to the player object
                var acidDeathPrefab = HeroController.instance.acidDeathPrefab;
                var acidDeath = acidDeathPrefab.Spawn(playerObject.transform.position);
                // Set the scale to the player scale
                acidDeath.transform.localScale = playerObject.transform.localScale;

                // Destroy it after some time
                Object.Destroy(acidDeath, 2.0f);
            }
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}