using System;
using Hkmp.Api.Server;
using Hkmp.Menu;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable StringLiteralTypo

namespace Hkmp.Game.Settings;

/// <inheritdoc cref="IServerSettings" />
public class ServerSettings : IServerSettings, IEquatable<ServerSettings> {
    /// <inheritdoc />
    [SettingAlias("pvp")]
    [MenuName("PvP")]
    [MenuDescription("Player versus Player damage")]
    public bool IsPvpEnabled { get; set; }

    /// <inheritdoc />
    [SettingAlias("bodydamage")]
    [MenuName("Body Damage")]
    [MenuDescription("If PvP is on, whether player hitboxes do damage")]
    public bool IsBodyDamageEnabled { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("globalmapicons")]
    [MenuName("Global Map Icons")]
    [MenuDescription("Always show map icons for all players")]
    public bool AlwaysShowMapIcons { get; set; }

    /// <inheritdoc />
    [SettingAlias("compassicon", "compassicons", "waywardicon", "waywardicons")]
    [MenuName("Wayward Compass Map Icons")]
    [MenuDescription("Only show map icons when Wayward Compass is equipped")]
    public bool OnlyBroadcastMapIconWithWaywardCompass { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("names")]
    [MenuName("Show Names")]
    [MenuDescription("Show names of player above their characters")]
    public bool DisplayNames { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("teams")]
    [MenuName("Teams")]
    [MenuDescription("Whether players can join teams")]
    public bool TeamsEnabled { get; set; }

    /// <inheritdoc />
    [SettingAlias("skins")]
    [MenuName("Skins")]
    [MenuDescription("Whether players can have skins")]
    public bool AllowSkins { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("parries")]
    [MenuName("Parries")]
    [MenuDescription("Whether parrying certain player attacks is possible")]
    public bool AllowParries { get; set; } = true;

    /// <inheritdoc />
    [SettingAlias("naildmg")]
    [MenuName("Nail Damage")]
    [MenuDescription("The number of masks of damage that a player's nail swing deals")]
    public byte NailDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("elegydmg")]
    [MenuName("Grubberfly's Elegy Damage")]
    [MenuDescription("The number of masks of damage that Grubberfly's Elegy deals")]
    public byte GrubberflyElegyDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("vsdmg", "fireballdamage", "fireballdmg")]
    [MenuName("Vengeful Spirit Damage")]
    [MenuDescription("The number of masks of damage that Vengeful Spirit deals")]
    public byte VengefulSpiritDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("shadesouldmg")]
    [MenuName("Shade Soul Damage")]
    [MenuDescription("The number of masks of damage that Shade Soul deals")]
    public byte ShadeSoulDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("desolatedivedmg", "ddivedmg")]
    [MenuName("Desolate Dive Damage")]
    [MenuDescription("The number of masks of damage that Desolate Dive deals")]
    public byte DesolateDiveDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("descendingdarkdmg", "ddarkdmg")]
    [MenuName("Descending Dark Damage")]
    [MenuDescription("The number of masks of damage that Descending Dark deals")]
    public byte DescendingDarkDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("howlingwraithsdamage", "howlingwraithsdmg", "wraithsdmg")]
    [MenuName("Howling Wraiths Damage")]
    [MenuDescription("The number of masks of damage that Howling Wraiths deals")]
    public byte HowlingWraithDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("abyssshriekdmg", "shriekdmg")]
    [MenuName("Abyss Shriek Damage")]
    [MenuDescription("The number of masks of damage that Abyss Shriek deals")]
    public byte AbyssShriekDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("greatslashdmg")]
    [MenuName("Great Slash Damage")]
    [MenuDescription("The number of masks of damage that Great Slash deals")]
    public byte GreatSlashDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("dashslashdmg")]
    [MenuName("Dash Slash Damage")]
    [MenuDescription("The number of masks of damage that Dash Slash deals")]
    public byte DashSlashDamage { get; set; } = 2;

    /// <inheritdoc />
    [SettingAlias("cycloneslashdmg", "cyclonedmg")]
    [MenuName("Cyclone Slash Damage")]
    [MenuDescription("The number of masks of damage that Cyclone Slash deals")]
    public byte CycloneSlashDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("sporeshroomdmg")]
    [MenuName("Spore Shroom Damage")]
    [MenuDescription("The number of masks of damage that a Spore Shroom cloud deals")]
    public byte SporeShroomDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("sporedungshroomdmg", "dungshroomdmg")]
    [MenuName("Spore-Dung Shroom Damage")]
    [MenuDescription("The number of masks of damage that a Spore Shroom cloud with Defender's Crest deals")]
    public byte SporeDungShroomDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("thornsofagonydamage", "thornsofagonydmg", "thornsdamage", "thornsdmg")]
    [MenuName("Thorns of Agongy Damage")]
    [MenuDescription("The number of masks of damage that the Thorns of Agony lash deals")]
    public byte ThornOfAgonyDamage { get; set; } = 1;

    /// <inheritdoc />
    [SettingAlias("sharpshadowdmg")]
    [MenuName("Sharp Shadow Damage")]
    [MenuDescription("The number of masks of damage that a Sharp Shadow dash deals")]
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

    /// <summary>
    /// Get a copy of this instance of the server settings.
    /// </summary>
    /// <returns>A new instance of the server settings with the same values as this instance.</returns>
    public ServerSettings GetCopy() {
        var serverSettings = new ServerSettings();
        serverSettings.SetAllProperties(this);

        return serverSettings;
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
