namespace HKMP.Game.Settings {
    /**
     * Settings related to gameplay that is shared between server and clients
     */
    public class GameSettings {
        public bool IsPvpEnabled { get; set; }
        public bool IsBodyDamageEnabled { get; set; }
        public bool AlwaysShowMapIcons { get; set; }
        public bool OnlyBroadcastMapIconWithWaywardCompass { get; set; }

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