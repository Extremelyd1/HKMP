using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for when the Cyclone Slash ability ends.
/// </summary>
internal class CycloneSlashEnd : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        RemoveCycloneSlash(playerObject);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }

    public static void RemoveCycloneSlash(GameObject playerObject) {
        // Get the remote player attacks object
        var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");
        // Find the object in the children of the attacks object
        var cycloneObject = playerAttacks.FindGameObjectInChildren("Cyclone Slash");
        if (cycloneObject != null) {
            // Destroy the Cyclone Slash object
            Object.Destroy(cycloneObject);
        }
    }
}
