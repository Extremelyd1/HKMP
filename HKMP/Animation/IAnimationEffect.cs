using HKMP.Networking.Packet.Custom;
using UnityEngine;

// TODO: maybe add a method that removes all existing/outstanding effect objects
// so we don't run into any runaway objects, we can do this when the scene changes
namespace HKMP.Animation {
    /**
     * Class that handlers animation effects that complement player animation.
     */
    public interface IAnimationEffect {
        /*
         * Plays the animation effect for the given player object and with data from the given Packet.
         */
        void Play(GameObject playerObject, bool[] effectInfo);

        /**
         * Prepares a packet by filling it with the necessary data for this effect.
         */
        bool[] GetEffectInfo();

        /**
         * Set the game settings so we can access it while playing the animation
         */
        void SetGameSettings(Game.Settings.GameSettings gameSettings);
    }
}