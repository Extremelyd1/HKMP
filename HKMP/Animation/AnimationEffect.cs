using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation {
    /// <summary>
    /// Abstract base class for animation effects.
    /// </summary>
    internal abstract class AnimationEffect : IAnimationEffect {
        /// <summary>
        /// The current GameSettings instance.
        /// </summary>
        protected Game.Settings.GameSettings GameSettings;

        /// <inheritdoc/>
        public abstract void Play(GameObject playerObject, bool[] effectInfo);

        /// <inheritdoc/>
        public abstract bool[] GetEffectInfo();

        /// <inheritdoc/>
        public void SetGameSettings(Game.Settings.GameSettings gameSettings) {
            GameSettings = gameSettings;
        }

        /// <summary>
        /// Locate the damages_enemy FSM and change the attack type to generic. This will avoid the local
        /// player taking knock back from remote players hitting shields etc.
        /// </summary>
        /// <param name="targetObject">The target GameObject to change.</param>
        protected static void ChangeAttackTypeOfFsm(GameObject targetObject) {
            var damageFsm = targetObject.LocateMyFSM("damages_enemy");
            if (damageFsm == null) {
                return;
            }

            var takeDamage = damageFsm.GetAction<TakeDamage>("Send Event", 8);
            takeDamage.AttackType.Value = (int) AttackTypes.Generic;
            takeDamage = damageFsm.GetAction<TakeDamage>("Parent", 6);
            takeDamage.AttackType.Value = (int) AttackTypes.Generic;
            takeDamage = damageFsm.GetAction<TakeDamage>("Grandparent", 6);
            takeDamage.AttackType.Value = (int) AttackTypes.Generic;
        }
    }
}