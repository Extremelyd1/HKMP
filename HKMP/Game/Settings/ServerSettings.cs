using System;
using Hkmp.Api.Server;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable StringLiteralTypo

namespace Hkmp.Game.Settings;

/// <inheritdoc cref="IServerSettings" />
public class ServerSettings : IServerSettings, IEquatable<ServerSettings> {
    /// <inheritdoc />
    [SettingAlias("pvp")]
    public bool IsPvpEnabled { get; set; }

    /// <inheritdoc />
    [SettingAlias("bodydamage")]
    public bool IsBodyDamageEnabled { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("globalmapicons")]
    public bool AlwaysShowMapIcons { get; set; }

    /// <inheritdoc />
    [SettingAlias("compassicon", "compassicons", "waywardicon", "waywardicons")]
    public bool OnlyBroadcastMapIconWithWaywardCompass { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("names")]
    public bool DisplayNames { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("teams")]
    public bool TeamsEnabled { get; set; }

    /// <inheritdoc />
    [SettingAlias("skins")]
    public bool AllowSkins { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("parries")]
    public bool AllowParries { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("naildmg")]
    public byte NailDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("elegydmg")]
    public byte GrubberflyElegyDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("vsdmg", "fireballdamage", "fireballdmg")]
    public byte VengefulSpiritDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("shadesouldmg")]
    public byte ShadeSoulDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("desolatedivedmg", "ddivedmg")]
    public byte DesolateDiveDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("descendingdarkdmg", "ddarkdmg")]
    public byte DescendingDarkDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("howlingwraithsdamage", "howlingwraithsdmg", "wraithsdmg")]
    public byte HowlingWraithDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("abyssshriekdmg", "shriekdmg")]
    public byte AbyssShriekDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("greatslashdmg")]
    public byte GreatSlashDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("dashslashdmg")]
    public byte DashSlashDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("cycloneslashdmg", "cyclonedmg")]
    public byte CycloneSlashDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("sporeshroomdmg")]
    public byte SporeShroomDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("sporedungshroomdmg", "dungshroomdmg")]
    public byte SporeDungShroomDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("thornsofagonydamage", "thornsofagonydmg", "thornsdamage", "thornsdmg")]
    public byte ThornOfAgonyDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("sharpshadowdmg")]
    public byte SharpShadowDamage { get; set; } = 1;

    /// <summary>
    /// Set all properties in this <see cref="ServerSettings"/> instance to the values from the given
    /// <see cref="ServerSettings"/> instance.
    /// </summary>
    /// <param name="serverSettings">The instance to copy from.</param>
    public void SetAllProperties(ServerSettings serverSettings) {
        // Use reflection to copy over all properties into this object
        foreach (var prop in GetType().GetProperties()) {
            if (!prop.CanRead || !prop.CanWrite) {
                continue;
            }

            prop.SetValue(this, prop.GetValue(serverSettings, null), null);
        }
    }

    /// <inheritdoc />
    public bool Equals(ServerSettings other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }
    
        if (ReferenceEquals(this, other)) {
            return true;
        }
    
        foreach (var prop in GetType().GetProperties()) {
            if (!prop.CanRead) {
                continue;
            }
    
            if (prop.GetValue(this) != prop.GetValue(other)) {
                return false;
            }
        }
    
        return true;
    }
    
    /// <inheritdoc />
    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }
    
        if (ReferenceEquals(this, obj)) {
            return true;
        }
    
        if (obj.GetType() != GetType()) {
            return false;
        }
    
        return Equals((ServerSettings) obj);
    }
    
    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            var hashCode = 0;
            var first = true;
            foreach (var prop in GetType().GetProperties()) {
                if (!prop.CanRead) {
                    continue;
                }
                
                var propHashCode = prop.GetValue(this).GetHashCode();
    
                if (first) {
                    hashCode = propHashCode;
                    first = false;
                    continue;
                }
    
                hashCode = (hashCode * 397) ^ propHashCode;
            }
    
            return hashCode;
        }
    }
    
    /// <summary>
    /// Indicates whether one <see cref="ServerSettings"/> is equal to another <see cref="ServerSettings"/>.
    /// </summary>
    /// <param name="left">The first <see cref="ServerSettings"/> to compare.</param>
    /// <param name="right">The second <see cref="ServerSettings"/> to compare.</param>
    /// <returns>true if <paramref name="left"/> is equal to <paramref name="right"/>; false otherwise.</returns>
    public static bool operator ==(ServerSettings left, ServerSettings right) {
        return Equals(left, right);
    }
    
    /// <summary>
    /// Indicates whether one <see cref="ServerSettings"/> is not equal to another <see cref="ServerSettings"/>.
    /// </summary>
    /// <param name="left">The first <see cref="ServerSettings"/> to compare.</param>
    /// <param name="right">The second <see cref="ServerSettings"/> to compare.</param>
    /// <returns>true if <paramref name="left"/> is not equal to <paramref name="right"/>; false otherwise.</returns>
    public static bool operator !=(ServerSettings left, ServerSettings right) {
        return !Equals(left, right);
    }
}
