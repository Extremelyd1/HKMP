namespace HKMP.Game.Settings {
    /**
     * Settings related to gameplay that is shared between server and clients
     */
    public class GameSettings {
        public bool IsPvpEnabled { get; set; }
        public bool IsBodyDamageEnabled { get; set; }

        public void SetAllProperties(GameSettings gameSettings) {
            IsPvpEnabled = gameSettings.IsPvpEnabled;
            IsBodyDamageEnabled = gameSettings.IsBodyDamageEnabled;
        }
    }
}