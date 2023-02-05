using System.Collections.Generic;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for getting hit (which is also getting stunned).
/// </summary>
internal class Stun : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        RemoveExistingEffects(playerObject);

        CancelFocusEffect(playerObject);

        // Check whether the carefree melody charm activated for the player
        var carefreeActivated = effectInfo[0];

        // Get the player effects object to put new effects in
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        if (!carefreeActivated) {
            if (!HandleShellAnimation(playerEffects)) {
                PlayDamageEffects(playerEffects);

                PlayHitSound(playerObject);
            }
        } else {
            PlayCarefreeEffect(playerEffects);
        }
    }

    /// <summary>
    /// Remove all existing effect for the given player object.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    private void RemoveExistingEffects(GameObject playerObject) {
        // Remove all effects/attacks/spells related animations
        MonoBehaviourUtil.DestroyAllChildren(playerObject.FindGameObjectInChildren("Attacks"));
        // Since we still need the baldur shell animation to play, we don't want to destroy it yet
        MonoBehaviourUtil.DestroyAllChildren(
            playerObject.FindGameObjectInChildren("Effects"),
            new List<string>(new[] {
                "Shell Animation",
                "Shell Animation Last"
            })
        );
        MonoBehaviourUtil.DestroyAllChildren(playerObject.FindGameObjectInChildren("Spells"));
    }

    /// <summary>
    /// Cancel the focus effect for the given player object if it exists.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    private void CancelFocusEffect(GameObject playerObject) {
        // If either the charge audio or the lines animation objects exist,
        // the player was probably focussing, so we start the Focus End effect
        if (playerObject.FindGameObjectInChildren("Charge Audio") != null ||
            playerObject.FindGameObjectInChildren("Lines Anim") != null) {
            AnimationManager.FocusEnd.Play(playerObject);
        }
    }

    /// <summary>
    /// Handle the Baldur Shell animation in case the player has the charm equipped.
    /// </summary>
    /// <param name="playerEffects">The GameObject for the player effects.</param>
    /// <returns>True if the shell animation was handled, false otherwise.</returns>
    private bool HandleShellAnimation(GameObject playerEffects) {
        // Find the shell animation if it exists
        var shellAnimation = playerEffects.FindGameObjectInChildren("Shell Animation");
        var lastShellHit = false;

        // It might be suffixed with "Last" if it was the last baldur hit the player could take
        if (shellAnimation == null) {
            shellAnimation = playerEffects.FindGameObjectInChildren("Shell Animation Last");
            lastShellHit = true;
        }

        if (shellAnimation == null) {
            return false;
        }

        // If either version was found, we need to play some animations and sounds
        // Get the sprite animator and play the correct sounds if the shell broke or not
        var shellAnimator = shellAnimation.GetComponent<tk2dSpriteAnimator>();
        if (lastShellHit) {
            shellAnimator.Play("Break");
        } else {
            shellAnimator.Play("Impact");
        }

        // Destroy the animation after some time either way
        Object.Destroy(shellAnimation, 1.5f);

        // Get a new audio object and source and play the blocker impact clip
        var audioObject = AudioUtil.GetAudioSourceObject(playerEffects);
        var audioSource = audioObject.GetComponent<AudioSource>();
        audioSource.clip = HeroController.instance.blockerImpact;
        audioSource.Play();

        // Also destroy this object after some time
        Object.Destroy(audioObject, 2.0f);

        // If it was the last hit, we spawn some debris (bits) that fly of the shell as it breaks
        if (lastShellHit) {
            var charmEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Charm Effects");
            var blockerShieldObject = charmEffects.FindGameObjectInChildren("Blocker Shield");
            var shellFsm = blockerShieldObject.LocateMyFSM("Control");

            // Since this is replicated 5 times in the FSM, we loop 5 times
            for (var i = 1; i < 6; i++) {
                var flingObjectAction = shellFsm.GetAction<FlingObjectsFromGlobalPool>("Bits", i);

                // These values are from the FSM
                var config = new FlingUtils.Config {
                    Prefab = flingObjectAction.gameObject.Value,
                    AmountMin = 2,
                    AmountMax = 2,
                    AngleMin = 40,
                    AngleMax = 140,
                    SpeedMin = 15,
                    SpeedMax = 22
                };

                // Spawn, fling and store the bits
                var spawnedBits = FlingUtils.SpawnAndFling(
                    config,
                    playerEffects.transform,
                    Vector3.zero
                );
                // Destroy all the bits after some time
                foreach (var bit in spawnedBits) {
                    Object.Destroy(bit, 2.0f);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Play damage effects for getting hit.
    /// </summary>
    /// <param name="playerEffects">The GameObject for the player effects.</param>
    private void PlayDamageEffects(GameObject playerEffects) {
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

    /// <summary>
    /// Play the getting hit sound.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    private void PlayHitSound(GameObject playerObject) {
        // TODO: maybe add an option for playing the hit sound as it is very uncanny
        // Being used to only hearing this when you get hit

        // Obtain the hit audio clip
        var heroAudioController = HeroController.instance.gameObject.GetComponent<HeroAudioController>();
        var takeHitClip = heroAudioController.takeHit.clip;

        // Get a new audio source and play the clip
        var takeHitAudioObject = AudioUtil.GetAudioSourceObject(playerObject);
        var takeHitAudioSource = takeHitAudioObject.GetComponent<AudioSource>();
        takeHitAudioSource.clip = takeHitClip;
        // Decrease volume, since otherwise it is quite loud in contrast to the local player hit sound
        takeHitAudioSource.volume = 0.5f;
        takeHitAudioSource.Play();

        Object.Destroy(takeHitAudioObject, 3.0f);
    }

    /// <summary>
    /// Play the Carefree Melody charm effect if the player has it equipped.
    /// </summary>
    /// <param name="playerEffects">The GameObject for the player effects.</param>
    private void PlayCarefreeEffect(GameObject playerEffects) {
        // Get the care free shield object from the HeroController and instantiate a copy
        var localCarefreeShield = HeroController.instance.carefreeShield;
        var carefreeShield = Object.Instantiate(
            localCarefreeShield,
            playerEffects.transform
        );
        carefreeShield.SetActive(true);

        // Get the original audio source and its clip
        var audioSource = carefreeShield.GetComponent<AudioSource>();
        var carefreeClip = audioSource.clip;

        // Destroy the original
        Object.Destroy(audioSource);

        // Replace it by a new one that bases volume off of distance and play the clip
        var newAudioObject = AudioUtil.GetAudioSourceObject(playerEffects);
        newAudioObject.GetComponent<AudioSource>().PlayOneShot(carefreeClip);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        // Whether the Carefree Melody charm effect is currently active
        var carefreeActive = HeroController.instance.carefreeShield.activeSelf;

        return new[] {
            carefreeActive
        };
    }
}
