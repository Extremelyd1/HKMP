using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hkmp.Serialization;

/// <summary>
/// Class that encompasses the MapZone enum from HK to allow (de)serialization on the server side (including the
/// standalone server).
/// </summary>
[JsonConverter(typeof(MapZoneConverter))]
public class MapZone {
    /// <summary>
    /// The (raw) byte value that represents the MapZone. 
    /// </summary>
    private byte Value { get; set; }

    /// <summary>
    /// Explicit conversion from the internal type to this type.
    /// </summary>
    /// <param name="mapZone">The internal-typed instance.</param>
    /// <returns>The converted instance of this type.</returns>
    public static explicit operator MapZone(GlobalEnums.MapZone mapZone) {
        return new MapZone {
            Value = (byte) mapZone
        };
    }

    /// <summary>
    /// Explicit conversion from this type to the internal type.
    /// </summary>
    /// <param name="mapZone">The instance of this type.</param>
    /// <returns>The converted instance of the internal type.</returns>
    public static explicit operator GlobalEnums.MapZone(MapZone mapZone) {
        return (GlobalEnums.MapZone) mapZone.Value;
    }

    /// <summary>
    /// Explicit conversion from a byte to this type.
    /// </summary>
    /// <param name="b">The byte.</param>
    /// <returns>The converted instance of this type.</returns>
    public static explicit operator MapZone(byte b) {
        return new MapZone {
            Value = b
        };
    }

    /// <summary>
    /// Explicit conversion from this type to a byte.
    /// </summary>
    /// <param name="mapZone">The instance of this type.</param>
    /// <returns>The converted byte.</returns>
    public static explicit operator byte(MapZone mapZone) {
        return mapZone.Value;
    }

    /// <summary>
    /// JSON converter class to handle converting MapZone values into and from JSON. 
    /// </summary>
    public class MapZoneConverter : JsonConverter {
        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value == null) {
                return;
            }

            var mapZone = (MapZone) value;

            var jValue = new JValue(mapZone.Value);
            jValue.WriteTo(writer);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var jToken = JToken.Load(reader);

            if (jToken is JValue { HasValues: true, Value: long longValue and >= 0 and <= 255 }) {
                return new MapZone {
                    Value = (byte) longValue
                };
            }

            return null;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(MapZone);
        }
    }
}
