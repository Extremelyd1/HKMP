using System.Collections;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Abstract base class for the animation effect of dashing.
/// </summary>
internal abstract class DashBase : DamageAnimationEffect {
    /// <inheritdoc/>
    public abstract override void Play(GameObject playerObject, bool[] effectInfo);

    /// <summary>
    /// Plays the dash animation for the given player object with the given effect info and booleans
    /// denoting what kind of dash it is.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <param name="effectInfo">A boolean array containing effect info.</param>
    /// <param name="shadowDash">Whether this dash is a shadow dash.</param>
    /// <param name="sharpShadow">Whether this dash is a sharp shadow dash.</param>
    /// <param name="dashDown">Whether this is a downwards dash.</param>
    protected void Play(GameObject playerObject, bool[] effectInfo, bool shadowDash, bool sharpShadow,
        bool dashDown) {
        // Obtain the dash audio clip
        var heroAudioController = HeroController.instance.gameObject.GetComponent<HeroAudioController>();
        var dashAudioClip = heroAudioController.dash.clip;

        // Get a new audio source and play the clip
        var dashAudioSourceObject = AudioUtil.GetAudioSourceObject(playerObject);
        var dashAudioSource = dashAudioSourceObject.GetComponent<AudioSource>();
        dashAudioSource.clip = dashAudioClip;
        dashAudioSource.Play();

        // Destroy the audio object after the clip is finished
        Object.Destroy(dashAudioSourceObject, dashAudioClip.length);

        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Store the transform and scale, because we need it later
        var playerTransform = playerObject.transform;
        var playerScale = playerTransform.localScale;

        // Check whether we are shadow dashing, because the animations differ quite a lot
        if (shadowDash) {
            // Get a fresh audio object
            var shadowDashAudioSourceObject = AudioUtil.GetAudioSourceObject(playerObject);
            var shadowDashAudioSource = shadowDashAudioSourceObject.GetComponent<AudioSource>();

            // Based on the sharp shadow charm, we set the correct clip
            shadowDashAudioSource.clip = sharpShadow
                ? HeroController.instance.sharpShadowClip
                : HeroController.instance.shadowDashClip;

            // And play the clip
            shadowDashAudioSource.Play();

            // Destroy the object after the clip is finished
            Object.Destroy(shadowDashAudioSourceObject, shadowDashAudioSource.clip.length);

            Vector3 spawnPosition;

            // Adjust the position based on whether we are dashing downwards
            if (dashDown) {
                spawnPosition = new Vector3(
                    0f, 3.5f, 0.00101f
                );
            } else {
                spawnPosition = new Vector3(
                    playerScale.x > 0 ? 5.21f : -5.21f, -0.58f, 0.00101f
                );
            }

            // Instantiate the dash effect relative to the player position
            var dashEffectPrefab = HeroController.instance.shadowdashBurstPrefab;
            var dashEffect = Object.Instantiate(dashEffectPrefab);
            Transform dashEffectTransform = dashEffect.transform;
            dashEffectTransform.position = playerTransform.position + spawnPosition;
            dashEffectTransform.rotation = playerTransform.rotation;
            dashEffectTransform.localScale = playerTransform.localScale;

            dashEffect.SetActive(true);
            Object.Destroy(dashEffect, 1.0f);
            if (dashDown) {
                // If we are performing a down dash, rotate the effect
                dashEffect.transform.localEulerAngles = new Vector3(0f, 0f, 270f);
            } else {
                // Set the scale based on the direction the player is facing
                var dashEffectScale = dashEffect.transform.localScale;
                dashEffect.transform.localScale = new Vector3(
                    playerScale.x > 0 ? 1.919591f : -1.919591f, dashEffectScale.y, dashEffectScale.z
                );
            }

            // Find the shadow dash particles in player effects, or create them
            // These are the dark blobs that show in a trail behind the dash of the knight
            var dashParticlesPrefab = HeroController.instance.shadowdashParticlesPrefab;
            var dashParticles = Object.Instantiate(
                dashParticlesPrefab,
                playerEffects.transform
            );

            // Give them a name, so we can reference them
            dashParticles.name = "Shadow Dash Particles";

            // Enable particle system emission, so the particles spawn
#pragma warning disable 0618
            dashParticles.GetComponent<ParticleSystem>().enableEmission = true;
#pragma warning restore 0618

            // As a failsafe, destroy them after 1.5 seconds
            Object.Destroy(dashParticles, 1.5f);

            // Spawn a shadow ring
            // This is the circle that quickly expands from the starting location of the dash 
            HeroController.instance.shadowRingPrefab.Spawn(playerEffects.transform);

            // Start a coroutine with the recharge animation, since we need to wait in it
            MonoBehaviourUtil.Instance.StartCoroutine(PlayRechargeAnimation(playerObject, playerEffects));

            var damage = ServerSettings.SharpShadowDamage;
            if (!sharpShadow) {
                // Lastly, disable the player collider, since we are in a shadow dash
                // We only do this, if we don't have sharp shadow
                playerObject.GetComponent<BoxCollider2D>().enabled = false;
            } else if (
                !ServerSettings.IsBodyDamageEnabled && 
                ServerSettings.IsPvpEnabled && 
                ShouldDoDamage && 
                damage != 0
            ) {
                // If body damage is disabled, but PvP is enabled and we are performing a sharp shadow dash
                // we need to enable the DamageHero component and move the player object to the correct layer
                // to allow the local player to collide with it
                playerObject.layer = 11;

                var damageHero = playerObject.GetComponent<DamageHero>();
                damageHero.enabled = true;
                damageHero.damageDealt = damage;
            }
        } else {
            // Instantiate the dash burst relative to the player effects
            var dashBurstObject = HeroController.instance.dashBurst.gameObject;
            var dashBurst = Object.Instantiate(
                dashBurstObject,
                playerEffects.transform
            );

            // Destroy the original FSM to prevent it from taking control of the animation
            Object.Destroy(dashBurst.LocateMyFSM("Effect Control"));
            dashBurst.SetActive(true);

            var dashBurstTransform = dashBurst.transform;

            // Set the position and rotation of the dash burst
            // These are all values from the HeroController HeroDash method
            if (dashDown) {
                dashBurstTransform.localPosition = new Vector3(-0.07f, 3.74f, 0.01f);
                dashBurstTransform.localEulerAngles = new Vector3(0f, 0f, 90f);
            } else {
                dashBurstTransform.localPosition = new Vector3(4.11f, -0.55f, 0.001f);
                dashBurstTransform.localEulerAngles = new Vector3(0f, 0f, 0f);
            }

            // Enable the mesh renderer
            dashBurst.GetComponent<MeshRenderer>().enabled = true;

            // Get the sprite animator and play the dash effect clip
            var dashBurstAnimator = dashBurst.GetComponent<tk2dSpriteAnimator>();
            dashBurstAnimator.Play("Dash Effect");

            // Destroy the object after the clip is finished
            Object.Destroy(dashBurst, dashBurstAnimator.GetClipByName("Dash Effect").Duration);

            // Find already existing dash particles object, or create a new one
            var dashParticlesPrefab = HeroController.instance.dashParticlesPrefab;
            var dashParticles = Object.Instantiate(
                dashParticlesPrefab,
                playerEffects.transform
            );


            // Give it a name, so we can reference it later
            dashParticles.name = "Dash Particles";

            // Start emitting the smoke cloud particles in the trail of the knight dash
#pragma warning disable 0618
            dashParticles.GetComponent<ParticleSystem>().enableEmission = true;
#pragma warning restore 0618

            // As a failsafe, destroy them after 0.75 seconds
            Object.Destroy(dashParticles, 0.75f);

            // If we are on the ground, we also spawn the dust cloud facing away from the knight
            if (effectInfo[0]) {
                var backDashEffect = HeroController.instance.backDashPrefab.Spawn(
                    playerObject.transform.position
                );
                backDashEffect.transform.localScale = new Vector3(
                    playerScale.x * -1f,
                    playerScale.y,
                    playerScale.z
                );
            }
        }
    }

    /// <summary>
    /// Plays the recharge animation of the dash.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <param name="playerEffects">The GameObject representing the player effect object within the player.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator PlayRechargeAnimation(GameObject playerObject, GameObject playerEffects) {
        yield return new WaitForSeconds(0.65f);

        var shadowRechargePrefab = HeroController.instance.shadowRechargePrefab;
        var rechargeFsm = shadowRechargePrefab.LocateMyFSM("Recharge Effect");

        // Obtain the recharge audio clip
        var audioPlayAction = rechargeFsm.GetFirstAction<AudioPlay>("Burst");
        var rechargeAudioClip = (AudioClip) audioPlayAction.oneShotClip.Value;

        // Get a new audio source and play the clip
        var rechargeAudioSourceObject = AudioUtil.GetAudioSourceObject(playerObject);
        var rechargeAudioSource = rechargeAudioSourceObject.GetComponent<AudioSource>();
        rechargeAudioSource.clip = rechargeAudioClip;
        rechargeAudioSource.Play();

        var rechargeObject = Object.Instantiate(
            shadowRechargePrefab,
            playerEffects.transform
        );

        Object.Destroy(rechargeObject.LocateMyFSM("Recharge Effect"));
        rechargeObject.SetActive(true);

        rechargeObject.GetComponent<MeshRenderer>().enabled = true;

        var rechargeAnimator = rechargeObject.GetComponent<tk2dSpriteAnimator>();
        rechargeAnimator.PlayFromFrame("Shadow Recharge", 0);

        yield return new WaitForSeconds(rechargeAnimator.GetClipByName("Shadow Recharge").Duration);

        Object.Destroy(rechargeObject);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return new[] { HeroController.instance.cState.onGround };
    }
}
