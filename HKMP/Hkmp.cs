using System;
using Hkmp.Api;
using Hkmp.Util;
using Modding;
using UnityEngine;
using ModSettings = Hkmp.Game.Settings.ModSettings;
using Object = UnityEngine.Object;

namespace Hkmp {
    // Main class of the mod
    public class Hkmp : Mod {
        private static HkmpApi _hkmpApi;
        
        // Statically create Settings object, so it can be accessed early
        private ModSettings _modSettings = new ModSettings();

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

            var gameManager = new Game.GameManager(_modSettings);

            _hkmpApi = new HkmpApi(gameManager);
        }

        public override Modding.ModSettings GlobalSettings {
            get => _modSettings;
            set => _modSettings = (ModSettings) value;
        }

        public static IHkmpApi GetApi() {
            if (_hkmpApi == null) {
                throw new InvalidOperationException(
                    "HKMP has not fully initialized yet, thus the API is not accessible");
            }
        
            return _hkmpApi;
        }
    }
}