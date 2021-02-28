using HKMP.Util;
using UnityEngine;

namespace HKMP.Fsm {
    public class FadeAudio {

        private readonly AudioSource _audioSource;

        private readonly float _startVolume;
        private readonly float _endVolume;
        private readonly float _time;

        private float _timeElapsed;
        private float _timeProgress;

        private readonly bool _fadingDown;

        public FadeAudio(AudioSource audioSource, float startVolume, float endVolume, float time) {
            _audioSource = audioSource;
            _startVolume = startVolume;
            _endVolume = endVolume;
            _time = time;

            _timeElapsed = 0;
            _timeProgress = 0;
            _fadingDown = _startVolume > _endVolume;
        }

        public void Update() {
            _timeElapsed += Time.deltaTime;
            _timeProgress = _timeElapsed / _time;

            _audioSource.volume += (_endVolume - _startVolume) * _timeProgress;

            if (_fadingDown && _audioSource.volume <= _endVolume) {
                MonoBehaviourUtil.Instance.OnUpdateEvent -= Update;
            }
            if (!_fadingDown && _audioSource.volume >= _endVolume) {
                MonoBehaviourUtil.Instance.OnUpdateEvent -= Update;
            }
        }

    }
}