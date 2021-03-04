using HKMP.Networking.Packet.Custom;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class CrystalDash : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Get both the local player and remote player effects object
            var heroEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Effects");
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");

            // Instantiate the crystal dash initial burst when launcher
            var sdBurst = Object.Instantiate(
                heroEffects.FindGameObjectInChildren("SD Burst"),
                playerEffects.transform
            );
            sdBurst.SetActive(true);

            // Make sure to destroy it once the FSM state machine is also done
            sdBurst.LocateMyFSM("FSM").InsertMethod("Destroy", 1, () => { Object.Destroy(sdBurst); });

            // Instantiate the crystal dash trail that is visible during the dash
            var sdTrail = Object.Instantiate(
                heroEffects.FindGameObjectInChildren("SD Trail"),
                playerEffects.transform
            );
            sdTrail.SetActive(true);
            // Give it a name, so we reference it later when it needs to be destroyed
            sdTrail.name = "SD Trail";
            
            // Again make sure to destroy it once FSM is done
            sdTrail.LocateMyFSM("FSM").InsertMethod("Destroy", 1, () => { Object.Destroy(sdTrail); });
            
            // Play the animation for the trail, so it isn't just a static texture behind the knight
            sdTrail.GetComponent<MeshRenderer>().enabled = true;
            sdTrail.GetComponent<tk2dSpriteAnimator>().PlayFromFrame("SD Trail", 0);
            
            // Calculate distance between local and remote player objects
            var distance = Vector3.Distance(playerObject.transform.position,
                HeroController.instance.gameObject.transform.position);

            // If this distance is smaller than the effect threshold, we play it
            // otherwise, players might see a glow from a crystal dash that is very far away
            if (distance < AnimationManager.EffectDistanceThreshold) {
                // Instantiate the glow object that flashes the screen once a crystal dash starts
                // According to FSM this object destroys itself after it is done
                var sdBurstGlow = Object.Instantiate(
                    heroEffects.FindGameObjectInChildren("SD Burst Glow"),
                    playerEffects.transform
                );
                sdBurstGlow.SetActive(true);
            }
        }

        // There is no extra data associated with this effect
        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}