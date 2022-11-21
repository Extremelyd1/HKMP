using System;
using MonoMod.ModInterop;

namespace Hkmp.Imports {
    /// <summary>
    /// A class to call functions from DebugMod using MonoMod.Interop. The functions will only run if DebugMod is loaded
    /// </summary>
    internal static class DebugMod {
        /// <summary>
        /// Tells MonoMod what functions to get from DebugMod
        /// </summary>
        [ModImportName("DebugMod")]
        private static class DebugImport {
            public static Action<bool> SetLockKeyBinds;
        }
        /// <summary>
        /// Loads the interop. MonoMod will automatically fill in the actions in DebugImport the first time they're used
        /// </summary>
        static DebugMod() {
            typeof(DebugImport).ModInterop();
        }
        /// <summary>
        /// Disables DebugMod keybinds when set to true and vice versa.
        /// </summary>
        /// <param name="value">The value to set lock keybinds.</param>
        public static void SetLockKeyBinds(bool value) => DebugImport.SetLockKeyBinds?.Invoke(value);
    }
}
