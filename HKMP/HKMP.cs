using System.Collections.Generic;
using HKMP.Util;
using Modding;
using UnityEngine;
using ModSettings = HKMP.Game.Settings.ModSettings;

namespace HKMP {
    // Main class of the mod
    public class HKMP : Mod {
        // Statically create Settings object, so it can be accessed early
        private ModSettings _modSettings = new ModSettings();

        public override string GetVersion() {
            return "0.5.0";
        }

        public override List<(string, string)> GetPreloadNames() {
            return new List<(string, string)> {
                ("GG_Hive_Knight", "Battle Scene/Hive Knight/Slash 1")
            };
        }

        public override void Initialize() {
            // Create a persistent gameObject where we can add the MonoBehaviourUtil to
            var gameObject = new GameObject("HKMP Persistent GameObject");
            Object.DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<MonoBehaviourUtil>();

            new Game.GameManager(_modSettings);
        }

        public override Modding.ModSettings GlobalSettings {
            get => _modSettings;
            set => _modSettings = (ModSettings) value;
        }
    }
}