using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public interface AnimationEffect {
        /*
         * Plays the animation effect for the given player object and with data from the given Packet.
         */
        void Play(GameObject playerObject, Packet packet);
    }
}