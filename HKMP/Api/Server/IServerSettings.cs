
namespace Hkmp.Api.Server;

/// <summary>
/// Settings related to gameplay that is shared between server and clients.
/// </summary>
public interface IServerSettings {
    /// <summary>
    /// Whether player vs. player damage is enabled.
    /// </summary>
    public bool IsPvpEnabled { get; }

    /// <summary>
    /// Whether player object will damage the local player.
    /// </summary>
    public bool IsBodyDamageEnabled { get; }

    /// <summary>
    /// Whether to always show map icons.
    /// </summary>
    public bool AlwaysShowMapIcons { get; }

    /// <summary>
    /// Whether to only broadcast the map icon of a player if they have wayward compass equipped.
    /// </summary>
    public bool OnlyBroadcastMapIconWithWaywardCompass { get; }

    /// <summary>
    /// Whether to display player names above the player objects.
    /// </summary>
    public bool DisplayNames { get; }

    /// <summary>
    /// Whether teams are enabled.
    /// </summary>
    public bool TeamsEnabled { get; }

    /// <summary>
    /// Whether skins are allowed.
    /// </summary>
    public bool AllowSkins { get; }
    
    /// <summary>
    /// Whether other player's attacks can be parried.
    /// </summary>
    public bool AllowParries { get; }

    /// <summary>
    /// The damage that nail swings from other players deal to the local player.
    /// </summary>
    public byte NailDamage { get; }

    /// <summary>
    /// The damage that the beam from Grubberfly's Elegy from other players deals to the local player.
    /// </summary>
    public byte GrubberflyElegyDamage { get; }

    /// <summary>
    /// The damage that Vengeful Spirit from other players deals to the local player.
    /// </summary>
    public byte VengefulSpiritDamage { get; }

    /// <summary>
    /// The damage that Shade Soul from other players deals to the local player.
    /// </summary>
    public byte ShadeSoulDamage { get; }

    /// <summary>
    /// The damage that Desolate Dive from other players deals to the local player.
    /// </summary>
    public byte DesolateDiveDamage { get; }

    /// <summary>
    /// The damage that Descending Dark from other players deals to the local player.
    /// </summary>
    public byte DescendingDarkDamage { get; }

    /// <summary>
    /// The damage that Howling Wraiths from other players deals to the local player.
    /// </summary>
    public byte HowlingWraithDamage { get; }

    /// <summary>
    /// The damage that Abyss Shriek from other players deals to the local player.
    /// </summary>
    public byte AbyssShriekDamage { get; }

    /// <summary>
    /// The damage that Great Slash from other players deals to the local player.
    /// </summary>
    public byte GreatSlashDamage { get; }

    /// <summary>
    /// The damage that Dash Slash from other players deals to the local player.
    /// </summary>
    public byte DashSlashDamage { get; }

    /// <summary>
    /// The damage that Cyclone Slash from other players deals to the local player.
    /// </summary>
    public byte CycloneSlashDamage { get; }

    /// <summary>
    /// The damage that the Spore Shroom cloud from other players deals to the local player.
    /// </summary>
    public byte SporeShroomDamage { get; }

    /// <summary>
    /// The damage that the Spore Shroom cloud with Defenders crest from other players deals to the local player.
    /// </summary>
    public byte SporeDungShroomDamage { get; }

    /// <summary>
    /// The damage that the activation of Thorns of Agony from other players deals to the local player.
    /// </summary>
    public byte ThornOfAgonyDamage { get; }

    /// <summary>
    /// The damage that a Sharp Shadow dash from others players deals to the local player.
    /// </summary>
    public byte SharpShadowDamage { get; }
}
