using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Thorns of Agony charm effect.
/// </summary>
internal class ThornsOfAgony : DamageAnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        var charmEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Charm Effects");
        if (charmEffects == null) {
            return;
        }

        // Find the Thorn Hit object
        var thornHitObject = charmEffects.FindGameObjectInChildren("Thorn Hit");
        if (thornHitObject == null) {
            return;
        }

        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Instantiate the Thorn Hit object relative to the player effects object
        var thornHit = Object.Instantiate(
            thornHitObject,
            playerEffects.transform
        );

        thornHit.SetActive(true);

        // Mirror the thorns if the player is flipped
        var thornScale = thornHit.transform.localScale;
        thornHit.transform.localScale = new Vector3(
            playerObject.transform.localScale.x > 0 ? 1 : -1,
            thornScale.y,
            thornScale.z
        );

        // For each child, add a DamageHero component when PvP is enabled
        var damage = ServerSettings.ThornOfAgonyDamage;
        if (ServerSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
            for (var i = 0; i < thornHit.transform.childCount; i++) {
                var child = thornHit.transform.GetChild(i).gameObject;
                child.AddComponent<DamageHero>().damageDealt = damage;
            }
        }

        // Destroy after 0.3 seconds as in the FSM
        Object.Destroy(thornHit, 0.3f);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
