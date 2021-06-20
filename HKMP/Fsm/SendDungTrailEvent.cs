using Hkmp.Animation;
using Hkmp.Networking.Client;
using UnityEngine;

namespace Hkmp.Fsm {
    public class SendDungTrailEvent {
        private const float Frequency = 0.75f;

        private readonly NetClient _netClient;

        private float _time;

        public SendDungTrailEvent(NetClient netClient) {
            _netClient = netClient;

            _time = 0;
        }

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

        public void Reset() {
            _time = 0;
        }
    }
}