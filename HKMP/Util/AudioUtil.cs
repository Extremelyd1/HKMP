using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace HKMP.Util {
    public static class AudioUtil {

        /**
         * Get an audio source relative to the given gameObject
         */
        public static GameObject GetAudioSourceObject(GameObject gameObject) {
            // Obtain the Nail Arts FSM from the Hero Controller
            var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");
            
            // Obtain the AudioSource from the AudioPlayerOneShotSingle action in the nail arts FSM
            var audioAction = nailArts.GetAction<AudioPlayerOneShotSingle>("Play Audio", 0);
            var audioPlayerObj = audioAction.audioPlayer.Value;

            return Object.Instantiate(
                audioPlayerObj,
                gameObject.transform
            );
        }
        
    }
}