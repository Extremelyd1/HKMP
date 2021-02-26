using System;
using HKMP.Networking.Packet;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using Object = UnityEngine.Object;
using ReflectionHelper = Modding.ReflectionHelper;

namespace HKMP.Animation {
    /**
     * Class for the start of both Desolate Dive and Descending Dark
     */
    public class DiveAntic : IAnimationEffect {
        public void Play(GameObject playerObject, Packet packet) {
            Logger.Info(this, "Dive 1");
            // Get the spell control object from the local player object
            var localSpellControl = HeroController.instance.spellControl;
            Logger.Info(this, "Dive 2");
            // Get the AudioPlay action from the Quake Antic state
            var quakeAnticAudioPlay = localSpellControl.GetAction<AudioPlay>("Quake Antic", 0);

            // A convoluted way of getting to an AudioSource so we can play the clip for this effect
            // I tried getting it from the AudioPlay object, but that one is always null for some reason
            // TODO: find a way to clean this up
            var spellControl = HeroController.instance.spellControl;
            var fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 1", 3).gameObject.Value;
            var fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            var audioAction = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 6);
            var audioPlayerObj = audioAction.audioPlayer.Value;
            var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
            var audioSource = audioPlayer.GetComponent<AudioSource>();
            
            // Lastly, we get the clip we need to play
            var quakeAnticClip = (AudioClip) quakeAnticAudioPlay.oneShotClip.Value;
            // Now we can play the clip
            audioSource.PlayOneShot(quakeAnticClip);

            // Get the remote player spell control object, to which we can assign the effect
            var playerSpellControl = playerObject.FindGameObjectInChildren("Spells");
            
            // Instantiate the Q Charge object from the prefab in the local spell control
            // Instantiate it relative to the remote player position
            var qCharge = Object.Instantiate(
                localSpellControl.gameObject.FindGameObjectInChildren("Q Charge"), 
                playerSpellControl.transform
            );
            qCharge.SetActive(true);
            // Set the name, so we can reference it later, when we need to destroy it
            qCharge.name = "Q Charge";

            // Start the animation at the first frame
            qCharge.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(0);
        }

        public void PreparePacket(Packet packet) {
        }
    }
}