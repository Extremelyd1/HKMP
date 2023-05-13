using Hkmp.Animation;
using Hkmp.Networking.Client;
using UnityEngine;

namespace Hkmp.Fsm;

/// <summary>
/// Class that messages sending the Defenders Crest charm trail as a periodic update.
/// </summary>
internal class SendDungTrailEvent {
    /// <summary>
    /// The frequency at which to send the update. 
    /// </summary>
    private const float Frequency = 0.75f;

    /// <summary>
    /// The net client instance.
    /// </summary>
    private readonly NetClient _netClient;

    /// <summary>
    /// Current elapsed time since the last update.
    /// </summary>
    private float _time;

    public SendDungTrailEvent(NetClient netClient) {
        _netClient = netClient;

        _time = 0;
    }

    /// <summary>
    /// Update the time and check whether we need to send another animation.
    /// </summary>
    public void Update() {
        _time += Time.deltaTime;
        if (_time < Frequency) {
            return;
        }

        _time = 0;

        // If we are not connected, we can't send anything
        if (!_netClient.IsConnected) {
            return;
        }

        _netClient.UpdateManager.UpdatePlayerAnimation(AnimationClip.DungTrail);
    }

    /// <summary>
    /// Resets the elapsed time to zero.
    /// </summary>
    public void Reset() {
        _time = 0;
    }
}
