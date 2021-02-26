using HKMP.Game;
using Modding;

namespace HKMP {
    // Main class of the mod
    public class HKMP : Mod {

        // Statically create Settings object, so it can be accessed early
        private Settings _settings = new Settings();
        
        public override string GetVersion() {
            return "0.0.1";
        }

        public override void Initialize() {
            var gameManager = new Game.GameManager(_settings);
        }

        public override ModSettings GlobalSettings {
            get => _settings;
            set => _settings = (Settings) value;
        }
    }
}