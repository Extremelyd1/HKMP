using Hkmp.Game.Settings;
using UnityEngine;

namespace Hkmp.Animation;

/// <summary>
/// Interface containing methods for handling animation effects that complement player animation.
/// </summary>
internal interface IAnimationEffect {
    /// <summary>
    /// Plays the animation effect for the given player object and with additional boolean data array.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <param name="effectInfo">A boolean array containing effect info.</param>
    void Play(GameObject playerObject, bool[] effectInfo);

    /// <summary>
    /// Get the effect info corresponding to this effect.
    /// </summary>
    /// <returns>A boolean array containing effect info.</returns>
    bool[] GetEffectInfo();

    /// <summary>
    /// Set the server settings so we can access it while playing the animation.
    /// </summary>
    /// <param name="serverSettings">The <see cref="ServerSettings"/> instance.</param>
    void SetServerSettings(ServerSettings serverSettings);
}
