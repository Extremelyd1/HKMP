using HKMP.Util;
using Modding;
using UnityEngine;
using ModSettings = HKMP.Game.Settings.ModSettings;

namespace HKMP {
    // Main class of the mod
    public class HKMP : Mod, IGlobalSettings<ModSettings> {
        // Statically create Settings object, so it can be accessed early
        private ModSettings _modSettings = new ModSettings();

        static HKMP() {
            // Set the logger to use the ModLog
            Logger.SetLogger(new ModLogger());
        }

        public override string GetVersion() {
            return Version.String;
        }

        public override void Initialize() {
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