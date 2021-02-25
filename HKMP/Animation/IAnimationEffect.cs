using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    /**
     * Class that handlers animation effects that complement player animation.
     */
    public interface IAnimationEffect {
        /*
         * Plays the animation effect for the given player object and with data from the given Packet.
         */
        void Play(GameObject playerObject, Packet packet);

        /**
         * Prepares a packet by filling it with the necessary data for this effect.
         */
        void PreparePacket(Packet packet);
    }
}