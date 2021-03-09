using HKMP.Networking.Packet.Custom;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using UnityEngine;

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
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}