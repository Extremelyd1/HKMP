using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the trail of the Defenders Crest charm.
/// </summary>
internal class DungTrail : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        var charmEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Charm Effects");
        if (charmEffects == null) {
            return;
        }

        var dungObject = charmEffects.FindGameObjectInChildren("Dung");
        if (dungObject == null) {
            return;
        }

        var dungControlFsm = dungObject.LocateMyFSM("Control");

        var spawnObjectAction = dungControlFsm.GetFirstAction<SpawnObjectFromGlobalPoolOverTime>("Equipped");

        // Spawn the dung trail object, which will despawn itself
        spawnObjectAction.gameObject.Value.Spawn(
            playerObject.transform.position,
            Quaternion.identity
        );

        // Check whether we have already created a dung particle, and if so, we don't need to create another
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");
        if (playerEffects.FindGameObjectInChildren("Dung Particle") != null) {
            return;
        }

        var setParticleEmissionAction = dungControlFsm.GetFirstAction<SetParticleEmission>("Emit Pause");
        var dungParticleEffect = Object.Instantiate(
            setParticleEmissionAction.gameObject.GameObject.Value,
            playerEffects.transform
        );
        dungParticleEffect.name = "Dung Particle";

#pragma warning disable 0618
        dungParticleEffect.GetComponent<ParticleSystem>().enableEmission = true;
#pragma warning restore 0618
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
