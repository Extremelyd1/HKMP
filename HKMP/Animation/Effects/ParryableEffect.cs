namespace Hkmp.Animation.Effects; 

/// <summary>
/// Represents an animation effect that can be parried, such as nail slashes or nail arts.
/// </summary>
internal abstract class ParryableEffect : DamageAnimationEffect {
    /// <summary>
    /// The FSM for the nail parry effect.
    /// </summary>
    protected readonly PlayMakerFSM NailClashTink;

    protected ParryableEffect() {
        var hiveKnightSlash = HkmpMod.PreloadedObjects["Hive_05"]["Battle Scene/Hive Knight/Slash 1"];
        NailClashTink = hiveKnightSlash.GetComponent<PlayMakerFSM>();
    }
}
