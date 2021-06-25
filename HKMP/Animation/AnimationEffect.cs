using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation {
    public abstract class AnimationEffect : IAnimationEffect {
        protected Game.Settings.GameSettings GameSettings;

        public abstract void Play(GameObject playerObject, bool[] effectInfo);

        public abstract bool[] GetEffectInfo();

        public void SetGameSettings(Game.Settings.GameSettings gameSettings) {
            GameSettings = gameSettings;
        }

        /**
         * Locate the damages_enemy FSM and change the attack type to generic.
         * This will avoid the local player taking knockback from remote players hitting shields etc.
         */
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