using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
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
            
            var superDashFsm = HeroController.instance.gameObject.LocateMyFSM("Superdash");

            var superDashAudioObject = playerObject.FindGameObjectInChildren("Superdash Audio");
            if (superDashAudioObject == null) {
                var dashAudioPlay = superDashFsm.GetAction<AudioPlay>("Dash Start", 5);
                var dashAudioSource = dashAudioPlay.gameObject.GameObject.Value.GetComponent<AudioSource>();

                superDashAudioObject = AudioUtil.GetAudioSourceObject(playerObject);
                superDashAudioObject.name = "Superdash Audio";
                superDashAudioObject.GetComponent<AudioSource>().clip = dashAudioSource.clip;
            }

            var dashBurstAudioPlay = superDashFsm.GetAction<AudioPlay>("Dash Start", 1);

            superDashAudioObject.GetComponent<AudioSource>().PlayOneShot((AudioClip) dashBurstAudioPlay.oneShotClip.Value);

            var crystalAudioPlayRandom = superDashFsm.GetAction<AudioPlayRandom>("Dash Start", 3);

            var randomIndex = new System.Random().Next(2);
            
            superDashAudioObject.GetComponent<AudioSource>().PlayOneShot(crystalAudioPlayRandom.audioClips[randomIndex]);

            // Play the audio source
            superDashAudioObject.GetComponent<AudioSource>().Play();
            
            var particleEmitAction = superDashFsm.GetAction<PlayParticleEmitter>("G Left", 0);
            var particleEmitter = Object.Instantiate(
                particleEmitAction.gameObject.GameObject.Value,
                playerEffects.transform
            );
            particleEmitter.name = "Dash Particle Emitter";
            particleEmitter.GetComponent<ParticleSystem>().Emit(100);
            
            Object.Destroy(particleEmitter, 2.0f);
        }

        // There is no extra data associated with this effect
        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}