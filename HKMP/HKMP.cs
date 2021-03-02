using System.Collections.Generic;
using HKMP.Game;
using HKMP.Util;
using Modding;
using UnityEngine;

namespace HKMP {
    // Main class of the mod
    public class HKMP : Mod {
        public static readonly Dictionary<string, GameObject> PreloadedObjects = new Dictionary<string, GameObject>();

        // Statically create Settings object, so it can be accessed early
        private Settings _settings = new Settings();

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

            var gameManager = new Game.GameManager(_settings);
        }

        public override ModSettings GlobalSettings {
            get => _settings;
            set => _settings = (Settings) value;
        }
    }
}