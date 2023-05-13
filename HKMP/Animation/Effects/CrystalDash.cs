using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Random = System.Random;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Crystal Dash ability.
/// </summary>
internal class CrystalDash : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Get both the local player and remote player effects object
        var heroEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Effects");
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        var sdBurstObject = heroEffects.FindGameObjectInChildren("SD Burst");

        if (sdBurstObject != null) {
            // Instantiate the crystal dash initial burst when launcher
            var sdBurst = Object.Instantiate(
                sdBurstObject,
                playerEffects.transform
            );
            sdBurst.SetActive(true);

            // Make sure to destroy it once the FSM state machine is also done
            sdBurst.LocateMyFSM("FSM").InsertMethod("Destroy", 1, () => { Object.Destroy(sdBurst); });
        }

        var sdTrailObject = heroEffects.FindGameObjectInChildren("SD Trail");

        if (sdTrailObject != null) {
            // Instantiate the crystal dash trail that is visible during the dash
            var sdTrail = Object.Instantiate(
                sdTrailObject,
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
        }

        var sdBurstGlowObject = heroEffects.FindGameObjectInChildren("SD Burst Glow");

        if (sdBurstGlowObject != null) {
            // Calculate distance between local and remote player objects
            var distance = Vector3.Distance(playerObject.transform.position,
                HeroController.instance.gameObject.transform.position);

            // If this distance is smaller than the effect threshold, we play it
            // otherwise, players might see a glow from a crystal dash that is very far away
            if (distance < AnimationManager.EffectDistanceThreshold) {
                // Instantiate the glow object that flashes the screen once a crystal dash starts
                // According to FSM this object destroys itself after it is done
                var sdBurstGlow = Object.Instantiate(
                    sdBurstGlowObject,
                    playerEffects.transform
                );
                sdBurstGlow.SetActive(true);
            }
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

        var dashBurstAudioPlay = superDashFsm.GetFirstAction<AudioPlay>("Dash Start");

        superDashAudioObject.GetComponent<AudioSource>()
            .PlayOneShot((AudioClip) dashBurstAudioPlay.oneShotClip.Value);

        var crystalAudioPlayRandom = superDashFsm.GetFirstAction<AudioPlayRandom>("Dash Start");

        var randomIndex = new Random().Next(2);

        superDashAudioObject.GetComponent<AudioSource>()
            .PlayOneShot(crystalAudioPlayRandom.audioClips[randomIndex]);

        // Play the audio source
        superDashAudioObject.GetComponent<AudioSource>().Play();

        var particleEmitAction = superDashFsm.GetFirstAction<PlayParticleEmitter>("G Left");
        var particleEmitter = Object.Instantiate(
            particleEmitAction.gameObject.GameObject.Value,
            playerEffects.transform
        );
        particleEmitter.name = "Dash Particle Emitter";
        particleEmitter.GetComponent<ParticleSystem>().Emit(100);

        Object.Destroy(particleEmitter, 2.0f);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        // There is no extra data associated with this effect
        return null;
    }
}
