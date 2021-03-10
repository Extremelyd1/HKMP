using HKMP.Networking.Packet.Custom;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using ReflectionHelper = Modding.ReflectionHelper;

namespace HKMP.Animation.Effects {
    public class FocusBurst : AnimationEffect {
        /**
         * The effect when the knight increases their health after healing
         */
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Get the local player spell control object
            var localSpellControl = HeroController.instance.spellControl;
            
            // Get the AudioSource from the audio action
            var audioAction = localSpellControl.GetAction<AudioPlayerOneShotSingle>("Focus Heal", 3);
            var audioPlayerObj = audioAction.audioPlayer.Value;
            var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
            var audioSource = audioPlayer.GetComponent<AudioSource>();
            
            // Get the audio clip of the Heal and play it
            var healClip = (AudioClip) audioAction.audioClip.Value;
            audioSource.PlayOneShot(healClip);

            // We don't need to audio player anymore
            Object.Destroy(audioPlayer, healClip.length);
            
            // Get the burst animation object through the Focus Heal state of the FSM
            var activateObjectAction = localSpellControl.GetAction<ActivateGameObject>("Focus Heal", 10);
            var burstAnimationObject = activateObjectAction.gameObject.GameObject.Value;

            // Instantiate it relative to the player object
            var burstAnimation = Object.Instantiate(
                burstAnimationObject,
                playerObject.transform
            );
            burstAnimation.SetActive(true);
            
            // Destroy after some time
            Object.DestroyObject(burstAnimation, 2.0f);

            var hasSporeShroom = packet.EffectInfo[0];
            var isSporeOnCooldown = packet.EffectInfo[3];

            // If the spore shroom is on cooldown or we don't have Spore Shroom charm equipped
            // there is no effect to be played, so we return
            if (isSporeOnCooldown || !hasSporeShroom) {
                return;
            }

            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            
            var hasDefenderCrest = packet.EffectInfo[1];
            var hasDeepFocus = packet.EffectInfo[2];
            
            // Since both Spore Cloud and Dung Cloud use the same structure, we find the correct FSM
            // and then spawn the correct object within that FSM
            GameObject objectVariant;
            
            if (hasDefenderCrest) {
                var spawnAction = localSpellControl.GetAction<SpawnObjectFromGlobalPool>("Dung Cloud", 0);
                objectVariant = spawnAction.gameObject.Value;
            } else {
                var spawnAction = localSpellControl.GetAction<SpawnObjectFromGlobalPool>("Spore Cloud", 3);
                objectVariant = spawnAction.gameObject.Value;
            }
            
            // Spawn the correct variant at the player position with default rotation
            var cloud = Object.Instantiate(
                objectVariant,
                playerEffects.transform.position,
                Quaternion.identity
            );
            cloud.SetActive(true);
            cloud.layer = 22;

            // Destroy the FSM so it doesn't use local player variables
            Object.Destroy(cloud.LocateMyFSM("Control"));

            // Since we destroyed the FSM, we need to mimic some of its behaviour
            // Such as activating the correct variant based on Deep Focus
            cloud.FindGameObjectInChildren("Pt Normal").SetActive(!hasDeepFocus);
            cloud.FindGameObjectInChildren("Pt Deep").SetActive(hasDeepFocus);

            // Set the scale based on whether we have Deep Focus
            if (hasDeepFocus) {
                cloud.transform.localScale = new Vector3(1.35f, 1.35f, 0f);
            } else {
                cloud.transform.localScale = new Vector3(1f, 1f, 0f);
            }

            // If PvP is enabled, add the DamageHero component
            if (GameSettings.IsPvpEnabled) {
                cloud.AddComponent<DamageHero>();
            }
                
            // Then after 4.1 seconds (as in the FSM), we remove it again
            Object.Destroy(cloud, 4.1f);
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
            var playerData = PlayerData.instance;
            packet.EffectInfo.Add(playerData.equippedCharm_17); // Spore Shroom
            packet.EffectInfo.Add(playerData.equippedCharm_10); // Defender's Crest
            packet.EffectInfo.Add(playerData.equippedCharm_34); // Deep Focus

            var sporeCooldownFsm = HeroController.instance.gameObject.LocateMyFSM("Spore Cooldown");
            if (sporeCooldownFsm == null) {
                packet.EffectInfo.Add(true); // True for Spore Shroom is on cooldown
                return;
            }
            
            // Since the event already happened locally, the FSM move to the Cooldown state
            // thus the only way to check whether we activated the cloud is when the cooldown is "fresh" aka ~0
            var timeOnCooldown = ReflectionHelper.GetAttr<Wait, float>(
                sporeCooldownFsm.GetAction<Wait>("Cooldown", 0), 
                "timer"
            );

            // We currently check for exactly a non-zero value
            // Perhaps we need to add a margin for edge cases
            packet.EffectInfo.Add(timeOnCooldown != 0);
        }
    }
}