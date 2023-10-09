using Hkmp.Game.Settings;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation;

/// <summary>
/// Abstract base class for animation effects.
/// </summary>
internal abstract class AnimationEffect : IAnimationEffect {
    /// <summary>
    /// The current <see cref="ServerSettings"/> instance.
    /// </summary>
    protected ServerSettings ServerSettings;

    /// <inheritdoc/>
    public abstract void Play(GameObject playerObject, bool[] effectInfo);

    /// <inheritdoc/>
    public abstract bool[] GetEffectInfo();

    /// <inheritdoc/>
    public void SetServerSettings(ServerSettings serverSettings) {
        ServerSettings = serverSettings;
    }

    /// <summary>
    /// Locate the damages_enemy FSM and change the attack type to generic. This will avoid the local
    /// player taking knock back from remote players hitting shields etc.
    /// </summary>
    /// <param name="targetObject">The target GameObject to change.</param>
    /// <param name="direction">The direction in float that the damage is coming from.</param>
    protected static void ChangeAttackTypeOfFsm(GameObject targetObject, float direction) {
        var damageFsm = targetObject.LocateMyFSM("damages_enemy");
        if (damageFsm == null) {
            return;
        }

        var takeDamage = damageFsm.GetFirstAction<TakeDamage>("Send Event");
        takeDamage.AttackType.Value = (int) AttackTypes.Generic;
        takeDamage = damageFsm.GetFirstAction<TakeDamage>("Parent");
        takeDamage.AttackType.Value = (int) AttackTypes.Generic;
        takeDamage = damageFsm.GetFirstAction<TakeDamage>("Grandparent");
        takeDamage.AttackType.Value = (int) AttackTypes.Generic;
        
        // Find the variable that controls the slash direction for damaging enemies
        var directionVar = damageFsm.FsmVariables.GetFsmFloat("direction");
        directionVar.Value = direction;
    }
}
