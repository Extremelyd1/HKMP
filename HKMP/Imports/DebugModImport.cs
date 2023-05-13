using System;
using MonoMod.ModInterop;

#pragma warning disable CS0649

namespace Hkmp.Imports;

/// <summary>
/// A class to call functions from DebugMod using MonoMod.Interop. The functions will only run if DebugMod is loaded.
/// </summary>
internal static class DebugMod {
    /// <summary>
    /// Static class with members to indicate to MonoMod which methods to get from DebugMod.
    /// </summary>
    [ModImportName("DebugMod")]
    private static class DebugImport {
        public static Action<bool> SetLockKeyBindsMethod;
    }

    /// <summary>
    /// Static constructor for loading the mod interop. MonoMod will automatically fill in the actions in DebugImport
    /// the first time they are used.
    /// </summary>
    static DebugMod() {
        typeof(DebugImport).ModInterop();
    }

    /// <summary>
    /// Sets whether or not key-binds for DebugMod are locked.
    /// </summary>
    /// <param name="value">Whether the key-binds should be locked or not.</param>
    public static void SetLockKeyBinds(bool value) => DebugImport.SetLockKeyBindsMethod?.Invoke(value);
}
