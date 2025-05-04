using System;
using Hkmp.Game.Client.Save;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hkmp.Game.Server.Save;

/// <summary>
/// JSON converter class to handle converting a list of entries for a mod save file into and from JSON.
/// </summary>
public class PlayerSaveDataConverter : JsonConverter {
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
        if (value == null) {
            return;
        }
        
        var entries = (ModSaveFile.PlayerDataEntries) value;
        
        // Create a JSON object as the basis for the list of entries
        var jObject = new JObject();

        // Then for each entry we add the name and value as a property to the object
        foreach (var entry in entries) {
            // The use of the 'serializer' in the FromObject is important to ensure we allow earlier defined converters
            // from acting on nested objects in the value of the entry (such as Unity's Vector3 being handled by
            // the modding API's Vector3 converter)
            jObject.Add(entry.Name, JToken.FromObject(entry.Value, serializer));
        }

        // Finally, write the JSON object to the writer
        jObject.WriteTo(writer);
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
        // We know that our JSON will have a JSON object, so we can read it
        var jObject = JObject.Load(reader);

        var entries = new ModSaveFile.PlayerDataEntries();

        // Loop over all properties of the object, since these are the entries in our ModSaveFile
        foreach (var prop in jObject.Properties()) {
            // Create the entry with the name only
            var entry = new ModSaveFile.PlayerDataEntry {
                Name = prop.Name
            };

            // Find the variable properties that correspond to the PlayerData variable name from this JSON property's
            // name
            if (!SaveDataMapping.Instance.PlayerDataVarProperties.TryGetValue(prop.Name, out var varProps)) {
                throw new ArgumentException(
                    $"Could not deserialize ModSaveFile.Entry, because variable '{prop.Name}' has no variable properties");
            }

            // From the variable properties, we obtain the type for the value of this JSON property
            var typeString = varProps.VarType;
            var type = Type.GetType(typeString);

            if (type == null) {
                throw new ArgumentException(
                    $"Could not deserialize ModSaveFile.Entry, because var type '{typeString}' could not be found");
            }

            // Then we can convert the JSON property's value
            // The use of the 'serializer' here is important to ensure we allow earlier defined converters from acting
            // on nested objects in the value of the entry (such as Unity's Vector3 being handled by the modding API's
            // Vector3 converter)
            entry.Value = prop.Value.ToObject(type, serializer);
            entries.Add(entry);
        }

        return entries;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType) {
        return objectType == typeof(ModSaveFile.PlayerDataEntry);
    }
}
