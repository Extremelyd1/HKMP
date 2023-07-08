﻿using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for when a dash ends.
/// </summary>
internal class DashEnd : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Enable the player collider again
        playerObject.GetComponent<BoxCollider2D>().enabled = true;
        // Disable the DamageHero component and reset the layer if body damage is disabled, but PvP is enabled
        // Because it might have been a shadow dash that was ended
        if (!ServerSettings.IsBodyDamageEnabled && ServerSettings.IsPvpEnabled) {
            playerObject.layer = 9;
            playerObject.GetComponent<DamageHero>().enabled = false;
        }

        var playerEffects = playerObject.FindGameObjectInChildren("Effects");
        if (playerEffects != null) {
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

        var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");
        if (playerAttacks != null) {
            var sharpShadow = playerAttacks.FindGameObjectInChildren("Sharp Shadow");
            if (sharpShadow != null) {
                Object.Destroy(sharpShadow);
            }
        }
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
