using Modding;

namespace HKMP {
    // Main class of the mod
    public class HKMP : Mod {
        
        public override string GetVersion() {
            return "0.0.1";
        }

        public override void Initialize() {
            var gameManager = new Game.GameManager();
        }
    }
}