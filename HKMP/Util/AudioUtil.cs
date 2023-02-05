using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Util;

/// <summary>
/// Static class proving utilities regarding audio.
/// </summary>
internal static class AudioUtil {
    /// <summary>
    /// Get an audio source relative to the given GameObject.
    /// </summary>
    /// <param name="gameObject">The GameObject to get an audio source relative to.</param>
    /// <returns>A GameObject with an audio source component.</returns>
    public static GameObject GetAudioSourceObject(GameObject gameObject) {
        // Obtain the Nail Arts FSM from the Hero Controller
        var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");

        // Obtain the AudioSource from the AudioPlayerOneShotSingle action in the nail arts FSM
        var audioAction = nailArts.GetFirstAction<AudioPlayerOneShotSingle>("Play Audio");
        var audioPlayerObj = audioAction.audioPlayer.Value;

        return Object.Instantiate(
            audioPlayerObj,
            gameObject.transform
        );
    }
}
