namespace Hkmp.Game.Settings {
    /// <summary>
    /// Settings related to gameplay that is shared between server and clients.
    /// </summary>
    internal class GameSettings {
        /// <summary>
        /// Whether player vs. player damage is enabled.
        /// </summary>
        public bool IsPvpEnabled { get; set; }
        /// <summary>
        /// Whether player object will damage the local player.
        /// </summary>
        public bool IsBodyDamageEnabled { get; set; } = true;

        /// <summary>
        /// Whether to always show map icons.
        /// </summary>
        public bool AlwaysShowMapIcons { get; set; }
        /// <summary>
        /// Whether to only broadcast the map icon of a player if they have wayward compass equipped.
        /// </summary>
        public bool OnlyBroadcastMapIconWithWaywardCompass { get; set; } = true;

        /// <summary>
        /// Whether to display player names above the player objects.
        /// </summary>
        public bool DisplayNames { get; set; } = true;

        /// <summary>
        /// Whether teams are enabled.
        /// </summary>
        public bool TeamsEnabled { get; set; }

        /// <summary>
        /// Whether skins are allowed.
        /// </summary>
        public bool AllowSkins { get; set; } = true;

        /// <summary>
        /// The damage that nail swings from other players deal to the local player.
        /// </summary>
        public byte NailDamage { get; set; } = 1;
        /// <summary>
        /// The damage that the beam from Grubberfly's Elegy from other players deals to the local player.
        /// </summary>
        public byte GrubberflyElegyDamage { get; set; } = 1;
        /// <summary>
        /// The damage that Vengeful Spirit from other players deals to the local player.
        /// </summary>
        public byte VengefulSpiritDamage { get; set; } = 1;
        /// <summary>
        /// The damage that Shade Soul from other players deals to the local player.
        /// </summary>
        public byte ShadeSoulDamage { get; set; } = 2;
        /// <summary>
        /// The damage that Desolate Dive from other players deals to the local player.
        /// </summary>
        public byte DesolateDiveDamage { get; set; } = 1;
        /// <summary>
        /// The damage that Descending Dark from other players deals to the local player.
        /// </summary>
        public byte DescendingDarkDamage { get; set; } = 2;
        /// <summary>
        /// The damage that Howling Wraiths from other players deals to the local player.
        /// </summary>
        public byte HowlingWraithDamage { get; set; } = 1;
        /// <summary>
        /// The damage that Abyss Shriek from other players deals to the local player.
        /// </summary>
        public byte AbyssShriekDamage { get; set; } = 2;
        /// <summary>
        /// The damage that Great Slash from other players deals to the local player.
        /// </summary>
        public byte GreatSlashDamage { get; set; } = 2;
        /// <summary>
        /// The damage that Dash Slash from other players deals to the local player.
        /// </summary>
        public byte DashSlashDamage { get; set; } = 2;
        /// <summary>
        /// The damage that Cyclone Slash from other players deals to the local player.
        /// </summary>
        public byte CycloneSlashDamage { get; set; } = 1;

        /// <summary>
        /// The damage that the Spore Shroom cloud from other players deals to the local player.
        /// </summary>
        public byte SporeShroomDamage { get; set; } = 1;
        /// <summary>
        /// The damage that the Spore Shroom cloud with Defenders crest from other players deals to the local player.
        /// </summary>
        public byte SporeDungShroomDamage { get; set; } = 1;
        /// <summary>
        /// The damage that the activation of Thorns of Agony from other players deals to the local player.
        /// </summary>
        public byte ThornOfAgonyDamage { get; set; } = 1;

        /// <summary>
        /// Set all properties in this GameSettings instance to the values from the given GameSettings instance.
        /// </summary>
        /// <param name="gameSettings">The instance to copy from.</param>
        public void SetAllProperties(GameSettings gameSettings) {
            // Use reflection to copy over all properties into this object
            foreach (var prop in GetType().GetProperties()) {
                if (!prop.CanRead || !prop.CanWrite) {
                    continue;
                }

                prop.SetValue(this, prop.GetValue(gameSettings, null), null);
            }
        }
    }
}