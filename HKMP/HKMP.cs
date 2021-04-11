using System.Collections.Generic;
using HKMP.Util;
using Modding;
using UnityEngine;
using ModSettings = HKMP.Game.Settings.ModSettings;

namespace HKMP {
    // Main class of the mod
    public class HKMP : Mod {
        public static readonly Dictionary<string, GameObject> PreloadedObjects = new Dictionary<string, GameObject>();

        // Statically create Settings object, so it can be accessed early
        private ModSettings _modSettings = new ModSettings();

        public override string GetVersion() {
            return "0.4.0 - ServerKnights v0.8b";
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