using Newtonsoft.Json;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Hkmp.Serialization;

/// <summary>
/// Class that mirrors BossStatue.Completion from HK to allow (de)serialization on the server side (including the
/// standalone server.
/// </summary>
public class BossStatueCompletion {
    [JsonProperty("hasBeenSeen")]
    public bool HasBeenSeen { get; set; }
    [JsonProperty("isUnlocked")]
    public bool IsUnlocked { get; set; }
    [JsonProperty("completedTier1")]
    public bool CompletedTier1 { get; set; }
    [JsonProperty("completedTier2")]
    public bool CompletedTier2 { get; set; }
    [JsonProperty("completedTier3")]
    public bool CompletedTier3 { get; set; }
    [JsonProperty("seenTier3Unlock")]
    public bool SeenTier3Unlock { get; set; }
    [JsonProperty("usingAltVersion")]
    public bool UsingAltVersion { get; set; }

    /// <summary>
    /// Explicit conversion from the internal type to this type.
    /// </summary>
    /// <param name="bsCompletion">The internal-typed instance.</param>
    /// <returns>The converted instance of this type.</returns>
    public static explicit operator BossStatueCompletion(BossStatue.Completion bsCompletion) {
        return new BossStatueCompletion {
            HasBeenSeen = bsCompletion.hasBeenSeen,
            IsUnlocked = bsCompletion.isUnlocked,
            CompletedTier1 = bsCompletion.completedTier1,
            CompletedTier2 = bsCompletion.completedTier2,
            CompletedTier3 = bsCompletion.completedTier3,
            SeenTier3Unlock = bsCompletion.seenTier3Unlock,
            UsingAltVersion = bsCompletion.usingAltVersion
        };
    }

    /// <summary>
    /// Explicit conversion from this type to the internal type.
    /// </summary>
    /// <param name="bsCompletion">The instance of this type.</param>
    /// <returns>The converted instance of the internal type.</returns>
    public static explicit operator BossStatue.Completion(BossStatueCompletion bsCompletion) {
        return new BossStatue.Completion {
            hasBeenSeen = bsCompletion.HasBeenSeen,
            isUnlocked = bsCompletion.IsUnlocked,
            completedTier1 = bsCompletion.CompletedTier1,
            completedTier2 = bsCompletion.CompletedTier2,
            completedTier3 = bsCompletion.CompletedTier3,
            seenTier3Unlock = bsCompletion.SeenTier3Unlock,
            usingAltVersion = bsCompletion.UsingAltVersion
        };
    }
}
