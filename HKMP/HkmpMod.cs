using Hkmp.Game.Settings;
using Hkmp.Util;
using JetBrains.Annotations;
using Modding;
using UnityEngine;

namespace Hkmp {
    /// <summary>
    /// Mod class for the HKMP mod.
    /// </summary>
    internal class HkmpMod : Mod, IGlobalSettings<ModSettings> {
        /// <summary>
        /// Statically create Settings object, so it can be accessed early.
        /// </summary>
        private ModSettings _modSettings = new ModSettings();

        /// <summary>
        /// Construct the HKMP mod.
        /// </summary>
        public HkmpMod() : base("HKMP") {
        }

        /// <inheritdoc />
        public override string GetVersion() {
            return Version.String;
        }

        /// <inheritdoc />
        public override void Initialize() {
            // Set the logger to use the ModLog
            Logger.SetLogger(new ModLogger());
            
            Logger.Get().Info(this, $"Initializing HKMP v{Version.String}");

            // Create a persistent gameObject where we can add the MonoBehaviourUtil to
            var gameObject = new GameObject("HKMP Persistent GameObject");
            Object.DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<MonoBehaviourUtil>();

            var gameManager = new Game.GameManager(_modSettings);
        }

        /// <inheritdoc />
        public void OnLoadGlobal(ModSettings modSettings) {
            _modSettings = modSettings ?? new ModSettings();
        }

        /// <inheritdoc />
        public ModSettings OnSaveGlobal() {
            return _modSettings;
        }
    }
}