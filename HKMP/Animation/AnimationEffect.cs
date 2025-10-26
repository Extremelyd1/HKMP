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
    /// Locate the damages_enemy FSM and change the attack direction to the given direciton. This will ensure that
    /// enemies are getting knocked back in the correct direction from remote player's attacks.
    /// </summary>
    /// <param name="targetObject">The target GameObject to change.</param>
    /// <param name="direction">The direction in float that the damage is coming from.</param>
    protected static void ChangeAttackDirection(GameObject targetObject, float direction) {
        var damageFsm = targetObject.LocateMyFSM("damages_enemy");
        if (damageFsm == null) {
            return;
        }
        
        // Find the variable that controls the slash direction for damaging enemies
        var directionVar = damageFsm.FsmVariables.GetFsmFloat("direction");
        directionVar.Value = direction;
    }
}
