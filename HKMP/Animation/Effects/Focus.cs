using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the focus ability (healing).
/// </summary>
internal class Focus : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Obtain the local player spell control object
        var localSpellControl = HeroController.instance.spellControl;

        // Get the AudioPlay action of the Focus Start state
        var chargeAudioPlay = localSpellControl.GetFirstAction<AudioPlay>("Focus Start");

        // Get the prefab object and instantiate it relative to the player
        var ownerDefaultTarget = chargeAudioPlay.Fsm.GetOwnerDefaultTarget(chargeAudioPlay.gameObject);
        var newChargeAudioObject = Object.Instantiate(
            ownerDefaultTarget,
            playerEffects.transform
        );
        // Set the name so we can reference it later
        newChargeAudioObject.name = "Charge Audio";

        // Get the AudioSource component and play it at volume 1
        var audio = newChargeAudioObject.GetComponent<AudioSource>();
        audio.Play();
        audio.volume = 1;

        // To prevent duplication of spawning the dust particles left and right
        var dustNames = new[] { "Dust L", "Dust R" };
        // The other dust particle action has a higher index
        var dustIndex = 9;
        foreach (var dustName in dustNames) {
            // Check whether the dust object is cached in the player
            var dust = playerObject.FindGameObjectInChildren(dustName);
            if (dust == null) {
                // It was not cached, so we create it
                var particleEmissionAction =
                    localSpellControl.GetAction<SetParticleEmissionRate>("Focus", dustIndex++);
                var dustLObject = particleEmissionAction.gameObject.GameObject.Value;
                dust = Object.Instantiate(
                    dustLObject,
                    playerEffects.transform
                );
                // Give it a name so we can reference it later
                dust.name = dustName;
            }

            // Get the particle system and start spawning dust particles
            var particleSystem = dust.GetComponent<ParticleSystem>();
            // Disable this warning since it is inherent to the Hollow Knight source, so we can't work around it
#pragma warning disable 0618
            particleSystem.emissionRate = 60;
            particleSystem.Play();
#pragma warning restore 0618
        }

        // Check whether the line animation is cached in the player object
        // This is the effect of white lines across the knight while healing
        var linesAnimation = playerObject.FindGameObjectInChildren("Lines Anim");
        if (linesAnimation == null) {
            // It was not cached, so we create it
            var meshRendererAction = localSpellControl.GetFirstAction<SetMeshRenderer>("Focus");
            var linesAnimationObject = meshRendererAction.gameObject.GameObject.Value;

            linesAnimation = Object.Instantiate(
                linesAnimationObject,
                playerEffects.transform
            );
            // Give it a name so we can reference it later
            linesAnimation.name = "Lines Anim";
        }

        // Enable the mesh renderer and play the Focus Effect animation
        linesAnimation.GetComponent<MeshRenderer>().enabled = true;
        linesAnimation.GetComponent<tk2dSpriteAnimator>().Play("Focus Effect");

        // As a failsafe, destroy object after some time if they are still active
        Object.Destroy(newChargeAudioObject, 5.0f);

        var baldurActive = effectInfo[0];
        var baldurLastHit = effectInfo[1];

        if (baldurActive) {
            var charmEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Charm Effects");
            if (charmEffects != null) {
                var blockerShieldObject = charmEffects.FindGameObjectInChildren("Blocker Shield");
                if (blockerShieldObject != null) {
                    var shellFsm = blockerShieldObject.LocateMyFSM("Control");
                    var playAnimationAction = shellFsm.GetFirstAction<Tk2dPlayAnimation>("Shell Up");

                    var shellAnimationObject = playAnimationAction.gameObject.GameObject.Value;
                    var shellAnimation = Object.Instantiate(
                        shellAnimationObject,
                        playerEffects.transform
                    );
                    if (baldurLastHit) {
                        shellAnimation.name = "Shell Animation Last";
                    } else {
                        shellAnimation.name = "Shell Animation";
                    }

                    var shellAnimator = shellAnimation.GetComponent<tk2dSpriteAnimator>();
                    shellAnimator.Stop();
                    shellAnimator.Play("Appear");

                    shellAnimation.GetComponent<MeshRenderer>().enabled = true;

                    var audioObject = AudioUtil.GetAudioSourceObject(playerEffects);
                    audioObject.name = "Shell Audio";
                    var audioSource = audioObject.GetComponent<AudioSource>();

                    var audioPlayAction = shellFsm.GetFirstAction<AudioPlayerOneShotSingle>("Shell Up");
                    audioSource.clip = (AudioClip) audioPlayAction.audioClip.Value;
                    audioSource.Play();

                    // As a failsafe, destroy objects after some time
                    Object.Destroy(shellAnimation, 10.0f);
                    Object.Destroy(audioObject, 10.0f);
                }
            }
        }
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        var playerData = PlayerData.instance;
        var blockerHits = playerData.GetInt(nameof(PlayerData.blockerHits));
        // Insert whether the Baldur Shell charm is equipped and we have hits left to tank
        return new[] {
            playerData.GetBool(nameof(PlayerData.equippedCharm_5)) && blockerHits > 0,
            blockerHits == 1
        };
    }
}
