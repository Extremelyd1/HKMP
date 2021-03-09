namespace HKMP.Game.Settings {
    /**
     * Settings related to gameplay that is shared between server and clients
     */
    public class GameSettings {
        public bool IsPvpEnabled { get; set; }
        public bool IsBodyDamageEnabled { get; set; }
        
        public bool AlwaysShowMapIcons { get; set; }
        public bool OnlyBroadcastMapIconWithWaywardCompass { get; set; }

        public bool DisplayNames { get; set; }

        public int NailDamage { get; set; }
        public int VengefulSpiritDamage { get; set; }
        public int ShadeSoulDamage { get; set; }
        public int DesolateDiveDamage { get; set; }
        public int DescendingDarkDamage { get; set; }
        public int HowlingWraithDamage { get; set; }
        public int AbyssShriekDamage { get; set; }
        public int GreatSlashDamage { get; set; }
        public int DashSlashDamage { get; set; }
        public int CycloneSlashDamage { get; set; }

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