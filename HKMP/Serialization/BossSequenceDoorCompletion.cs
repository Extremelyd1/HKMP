using System.Collections.Generic;
using Newtonsoft.Json;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Hkmp.Serialization;

/// <summary>
/// Class that mirrors BossSequenceDoor.Completion from HK to allow (de)serialization on the server side (including
/// the standalone server).
/// </summary>
public class BossSequenceDoorCompletion {
    [JsonProperty("canUnlock")]
    public bool CanUnlock { get; set; }
    [JsonProperty("unlocked")]
    public bool Unlocked { get; set; }
    [JsonProperty("completed")]
    public bool Completed { get; set; }
    [JsonProperty("allBindings")]
    public bool AllBindings { get; set; }
    [JsonProperty("noHits")]
    public bool NoHits { get; set; }
    [JsonProperty("boundNail")]
    public bool BoundNail { get; set; }
    [JsonProperty("boundShell")]
    public bool BoundShell { get; set; }
    [JsonProperty("boundCharms")]
    public bool BoundCharms { get; set; }
    [JsonProperty("boundSoul")]
    public bool BoundSoul { get; set; }
    [JsonProperty("viewedBossSceneCompletions")]
    public List<string> ViewedBossSceneCompletions { get; set; }

    /// <summary>
    /// Explicit conversion from the internal type to this type.
    /// </summary>
    /// <param name="bsdCompletion">The internal-typed instance.</param>
    /// <returns>The converted instance of this type.</returns>
    public static explicit operator BossSequenceDoorCompletion(BossSequenceDoor.Completion bsdCompletion) {
        return new BossSequenceDoorCompletion {
            CanUnlock = bsdCompletion.canUnlock,
            Unlocked = bsdCompletion.unlocked,
            Completed = bsdCompletion.completed,
            AllBindings = bsdCompletion.allBindings,
            NoHits = bsdCompletion.noHits,
            BoundNail = bsdCompletion.boundNail,
            BoundShell = bsdCompletion.boundShell,
            BoundCharms = bsdCompletion.boundCharms,
            BoundSoul = bsdCompletion.boundSoul,
            ViewedBossSceneCompletions = bsdCompletion.viewedBossSceneCompletions == null 
                ? [] 
                : [..bsdCompletion.viewedBossSceneCompletions]
        };
    }

    /// <summary>
    /// Explicit conversion from this type to the internal type.
    /// </summary>
    /// <param name="bsdCompletion">The instance of this type.</param>
    /// <returns>The converted instance of the internal type.</returns>
    public static explicit operator BossSequenceDoor.Completion(BossSequenceDoorCompletion bsdCompletion) {
        return new BossSequenceDoor.Completion {
            canUnlock = bsdCompletion.CanUnlock,
            unlocked = bsdCompletion.Unlocked,
            completed = bsdCompletion.Completed,
            allBindings = bsdCompletion.AllBindings,
            noHits = bsdCompletion.NoHits,
            boundNail = bsdCompletion.BoundNail,
            boundShell = bsdCompletion.BoundShell,
            boundCharms = bsdCompletion.BoundCharms,
            boundSoul = bsdCompletion.BoundSoul,
            viewedBossSceneCompletions = bsdCompletion.ViewedBossSceneCompletions == null 
                ? [] 
                : [..bsdCompletion.ViewedBossSceneCompletions]
        };
    }
}
