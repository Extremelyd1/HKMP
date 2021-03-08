namespace HKMP.Game.Settings {
    /**
     * Settings related to gameplay that is shared between server and clients
     */
    public class GameSettings {
        public bool IsPvpEnabled { get; set; }
        public bool IsBodyDamageEnabled { get; set; } = true;
        
        public bool AlwaysShowMapIcons { get; set; }
        public bool OnlyBroadcastMapIconWithWaywardCompass { get; set; } = true;
        
        public int NailDamage { get; set; } = 1;
        public int VengefulSpiritDamage { get; set; } = 1;
        public int ShadeSoulDamage { get; set; } = 2;
        public int DesolateDiveDamage { get; set; } = 1;
        public int DescendingDarkDamage { get; set; } = 2;
        public int HowlingWraithDamage { get; set; } = 1;
        public int AbyssShriekDamage { get; set; } = 2;
        public int GreatSlashDamage { get; set; } = 2;
        public int DashSlashDamage { get; set; } = 2;
        public int CycloneSlashDamage { get; set; } = 1;

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