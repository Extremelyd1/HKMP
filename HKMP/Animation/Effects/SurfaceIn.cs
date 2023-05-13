using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for entering water/acid.
/// </summary>
internal class SurfaceIn : AnimationEffect {
    /// <inheritdoc />
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        CycloneSlashEnd.RemoveCycloneSlash(playerObject);

        // Get the player spells object
        var playerSpells = playerObject.FindGameObjectInChildren("Spells");
        // Find the Q Trail object if it exists, this is the desolate dive downwards animation
        var qTrail = playerSpells.FindGameObjectInChildren("Q Trail");
        if (qTrail != null) {
            Object.Destroy(qTrail);
        }

        // Find the Q Trail 2 object if it exists, this is the descending dark downwards animation
        var qTrail2 = playerSpells.FindGameObjectInChildren("Q Trail 2");
        if (qTrail2 != null) {
            Object.Destroy(qTrail2);
        }
    }

    /// <inheritdoc />
    public override bool[] GetEffectInfo() {
        return null;
    }
}
