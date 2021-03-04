using System.Collections.Generic;
using HKMP.Game;
using HKMP.Util;
using Modding;
using UnityEngine;
using ModSettings = HKMP.Game.ModSettings;

namespace HKMP {
    // Main class of the mod
    public class HKMP : Mod {
        public static readonly Dictionary<string, GameObject> PreloadedObjects = new Dictionary<string, GameObject>();

        // Statically create Settings object, so it can be accessed early
        private ModSettings _modSettings = new ModSettings();

        public override string GetVersion() {
            return "0.0.1";
        }

        public override List<(string, string)> GetPreloadNames() {
            return new List<(string, string)> {
                ("GG_Hive_Knight", "Battle Scene/Hive Knight/Slash 1")
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
            // Store the preloaded object in a dictionary for easy access
            PreloadedObjects.Add("HiveKnightSlash",
                preloadedObjects["GG_Hive_Knight"]["Battle Scene/Hive Knight/Slash 1"]);

            GameManager.instance.gameObject.AddComponent<MonoBehaviourUtil>();

            var gameManager = new Game.GameManager(_modSettings);
        }

        public override Modding.ModSettings GlobalSettings {
            get => _modSettings;
            set => _modSettings = (ModSettings) value;
        }
    }
}