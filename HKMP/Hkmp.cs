using Hkmp.Util;
using Modding;
using UnityEngine;
using ModSettings = Hkmp.Game.Settings.ModSettings;

namespace Hkmp {
    // Main class of the mod
    public class Hkmp : Mod, IGlobalSettings<ModSettings> {
        // Statically create Settings object, so it can be accessed early
        private ModSettings _modSettings = new ModSettings();

        public Hkmp() : base("HKMP") {
        }

        public override string GetVersion() {
            return Version.String;
        }

        public override void Initialize() {
            // Set the logger to use the ModLog
            Logger.SetLogger(new ModLogger());
            
            Logger.Get().Info(this, $"Initializing HKMP v{Version.String}");

            // Create a persistent gameObject where we can add the MonoBehaviourUtil to
            var gameObject = new GameObject("HKMP Persistent GameObject");
            Object.DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<MonoBehaviourUtil>();

            new Game.GameManager(_modSettings);
        }

        public void OnLoadGlobal(ModSettings modSettings) {
            _modSettings = modSettings;
        }

        public ModSettings OnSaveGlobal() {
            return _modSettings;
        }
    }
}