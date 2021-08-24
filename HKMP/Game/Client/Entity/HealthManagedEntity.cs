using System;
using System.Collections.Generic;
using Hkmp.Networking.Client;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {
    public abstract class HealthManagedEntity : Entity {
        private const byte DieAnimationIndex = 255;

        // The HealthManager component of the entity
        private readonly HealthManager _healthManager;

        // Whether this entity is allowed to die, this is to prevent the hook
        // from interfering with a manual call to the health manager
        private bool _allowDeath;
        
        protected HealthManagedEntity(
            NetClient netClient, 
            EntityType entityType, 
            byte entityId, 
            GameObject gameObject) : base(netClient, entityType, entityId, gameObject) {
            _healthManager = gameObject.GetComponent<HealthManager>();
            
            On.HealthManager.Die += HealthManagerOnDieHook;
        }
        
        private void HealthManagerOnDieHook(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion) {
            if (self != _healthManager) {
                orig(self, attackDirection, attackType, ignoreEvasion);
                return;
            }

            if (!IsHostEntity) {
                if (!_allowDeath) {
                    return;
                }

                orig(self, attackDirection, attackType, ignoreEvasion);
                return;
            }

            var variables = new List<byte>();

            variables.AddRange(attackDirection.HasValue
                ? BitConverter.GetBytes(attackDirection.Value)
                : BitConverter.GetBytes(0f));
            variables.Add((byte) attackType);
            variables.AddRange(BitConverter.GetBytes(ignoreEvasion));
                
            Logger.Get().Info(this, $"Sending Die state with variables ({variables.Count} bytes): {attackDirection}, {attackType}, {ignoreEvasion}");

            SendAnimationUpdate(DieAnimationIndex, variables);

            orig(self, attackDirection, attackType, ignoreEvasion);

            Destroy();
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            if (animationIndex != 255) {
                return;
            }

            if (animationInfo.Length == 6) {
                float? directionFloat = BitConverter.ToSingle(animationInfo, 0);
                var attackType = (AttackTypes) animationInfo[4];
                var ignoreEvasion = BitConverter.ToBoolean(animationInfo, 5);
                        
                Logger.Get().Info(this, $"Received Die state with variable: {directionFloat}, {attackType}, {ignoreEvasion}");
        
                _allowDeath = true;
                _healthManager.Die(directionFloat, attackType, ignoreEvasion);
                        
                // We destroy after death to make sure we don't interfere with anything else
                Destroy();
            } else {
                Logger.Get().Info(this, $"Received Die state with incorrect variable array, length: {animationInfo.Length}");
            }
        }
        
        public override void Destroy() {
            base.Destroy();

            On.HealthManager.Die -= HealthManagerOnDieHook;
        }
    }
}