using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Fsm;

/// <summary>
/// Class for fading audio from an audio source over time.
/// </summary>
internal class FadeAudio {
    /// <summary>
    /// The audio source to fade.
    /// </summary>
    private readonly AudioSource _audioSource;

    /// <summary>
    /// The volume the audio source should start at.
    /// </summary>
    private readonly float _startVolume;

    /// <summary>
    /// The volume the audio source should end at.
    /// </summary>
    private readonly float _endVolume;

    /// <summary>
    /// The time it should take to fully fade out.
    /// </summary>
    private readonly float _time;

    /// <summary>
    /// Current elapsed time.
    /// </summary>
    private float _timeElapsed;

    /// <summary>
    /// Percentage of time that is elapsed.
    /// </summary>
    private float _timeProgress;

    /// <summary>
    /// Whether we are fading down or fading up the audio.
    /// </summary>
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

    /// <summary>
    /// Updates the audio source based on elapsed time.
    /// </summary>
    public void Update() {
        if (_audioSource == null) {
            MonoBehaviourUtil.Instance.OnUpdateEvent -= Update;
            return;
        }

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
