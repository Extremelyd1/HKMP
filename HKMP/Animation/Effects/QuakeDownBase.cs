using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Abstract base class for animation effect for the movement of going down from a either Desolate Dive or Descending Dark.
/// </summary>
internal abstract class QuakeDownBase : AnimationEffect {
    /// <inheritdoc/>
    public abstract override void Play(GameObject playerObject, bool[] effectInfo);

    /// <summary>
    /// Plays the quake down animation for the given player object, effect info and name of the trail prefab.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <param name="effectInfo">A boolean array containing effect info.</param>
    /// <param name="qTrailPrefabName">The name of the quake trail prefab.</param>
    protected void Play(GameObject playerObject, bool[] effectInfo, string qTrailPrefabName) {
        // Obtain the local player spell control object
        var localPlayerSpells = HeroController.instance.spellControl.gameObject;
        // Get the remote player spell object
        var playerSpells = playerObject.FindGameObjectInChildren("Spells");
        // Get the remote player effects object
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Instantiate the Q Flash Start from the prefab in the remote player spells object
        var qFlashStart = Object.Instantiate(
            localPlayerSpells.FindGameObjectInChildren("Q Flash Start"),
            playerSpells.transform
        );
        qFlashStart.SetActive(true);
        // And destroy it after a second
        Object.Destroy(qFlashStart, 1);

        // Instantiate the SD Sharp Flash from the prefab in the remote player effects object
        var sdSharpFlash = Object.Instantiate(
            localPlayerSpells.FindGameObjectInChildren("SD Sharp Flash"),
            playerEffects.transform
        );
        sdSharpFlash.SetActive(true);
        // And destroy it after a second
        Object.Destroy(sdSharpFlash, 1);

        // Instantiate the trail object from the prefab and spawn it in the remote player spells object
        // This is the texture that the knight has continually while diving down
        var qTrail = Object.Instantiate(
            localPlayerSpells.FindGameObjectInChildren(qTrailPrefabName),
            playerSpells.transform
        );
        qTrail.SetActive(true);
        // Assign a name so we reference it later, when we need to delete it
        qTrail.name = qTrailPrefabName;

        // Destroy the existing Q Charge from the antic
        Object.Destroy(playerSpells.FindGameObjectInChildren("Q Charge"));
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
