namespace Hkmp.Game.Settings {
    /**
     * Settings related to gameplay that is shared between server and clients
     */
    public class GameSettings {
        public bool IsPvpEnabled { get; set; }
        public bool IsBodyDamageEnabled { get; set; } = true;

        public bool AlwaysShowMapIcons { get; set; }
        public bool OnlyBroadcastMapIconWithWaywardCompass { get; set; } = true;

        public bool DisplayNames { get; set; } = true;

        public bool TeamsEnabled { get; set; }

        public bool AllowSkins { get; set; } = true;

        public bool SyncEntities { get; set; }

        public byte NailDamage { get; set; } = 1;
        public byte GrubberflyElegyDamage { get; set; } = 1;
        public byte VengefulSpiritDamage { get; set; } = 1;
        public byte ShadeSoulDamage { get; set; } = 2;
        public byte DesolateDiveDamage { get; set; } = 1;
        public byte DescendingDarkDamage { get; set; } = 2;
        public byte HowlingWraithDamage { get; set; } = 1;
        public byte AbyssShriekDamage { get; set; } = 2;
        public byte GreatSlashDamage { get; set; } = 2;
        public byte DashSlashDamage { get; set; } = 2;
        public byte CycloneSlashDamage { get; set; } = 1;

        public byte SporeShroomDamage { get; set; } = 1;
        public byte SporeDungShroomDamage { get; set; } = 1;
        public byte ThornOfAgonyDamage { get; set; } = 1;

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