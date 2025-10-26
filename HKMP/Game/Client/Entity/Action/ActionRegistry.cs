using System.Collections.Generic;
using System.Linq;
using Hkmp.Util;
using HutongGames.PlayMaker;
using Newtonsoft.Json;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity.Action; 

/// <summary>
/// Static class that manages loading and storing of action data. Specifically, which actions execute every frame.
/// </summary>
internal static class ActionRegistry {
    /// <summary>
    /// The file path of the embedded resource file for the action registry.
    /// </summary>
    private const string ActionRegistryFilePath = "Hkmp.Resource.action-registry.json";
    
    /// <summary>
    /// List of all entity registry entries that are loaded from the embedded file.
    /// </summary>
    private static List<ActionRegistryEntry> Entries { get; }

    static ActionRegistry() {
        Entries = FileUtil.LoadObjectFromEmbeddedJson<List<ActionRegistryEntry>>(ActionRegistryFilePath);
        if (Entries == null) {
            Logger.Warn("Could not load action registry");
        }
    }

    /// <summary>
    /// Checks whether the given FSM action is an action that executes continuously. This is the case for actions
    /// that have the "everyFrame" field and the value for this field is true or in several other specific cases
    /// (such as actions related to collisions).
    /// </summary>
    /// <param name="action">The FSM action to check.</param>
    /// <returns>true if the action executes continuously; otherwise false.</returns>
    public static bool IsActionContinuous(FsmStateAction action) {
        var entry = Entries.FirstOrDefault(entry => entry.Type == action.GetType().Name);
        if (entry == null) {
            return false;
        }

        if (entry.UpdateField == null) {
            return true;
        }

        var type = action.GetType();
        var fieldInfo = type.GetField(entry.UpdateField);
        if (fieldInfo == null) {
            Logger.Warn($"Could not find field on FSM state action class: {type}, {entry.UpdateField}");
            return false;
        }

        var value = fieldInfo.GetValue(action);
        if (value is bool boolValue) {
            return boolValue;
        }

        if (value is FsmInt fsmIntValue) {
            return fsmIntValue.Value > 0;
        }

        Logger.Warn($"Could not find type of the field on FSM state action class: {type}, {entry.UpdateField}");
        return false;
    }
}

/// <summary>
/// Class representing a single entry in the action registry that contains the relevant data for an action.
/// </summary>
internal class ActionRegistryEntry {
    /// <summary>
    /// The type of the action.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; }
    
    /// <summary>
    /// The name of the field that controls whether the actions is checked every frame.
    /// </summary>
    [JsonProperty("update_field")]
    public string UpdateField { get; set; }
}
