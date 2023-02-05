using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hkmp.Api.Addon;

namespace Hkmp.Api.Server;

/// <summary>
/// Addon loader for the server-side.
/// </summary>
internal class ServerAddonLoader : AddonLoader {
    /// <summary>
    /// Loads all server addons.
    /// </summary>
    /// <returns>A list of ServerAddon instances.</returns>
    public List<ServerAddon> LoadAddons() {
        return LoadAddons<ServerAddon>();
    }

    /// <inheritdoc/>
    protected override string GetCurrentDirectoryPath() {
        // We first try to get the entry assembly in case the executing assembly was
        // embedded in the standalone server
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null) {
            // If the entry assembly doesn't exist, we fall back on the executing assembly
            assembly = Assembly.GetExecutingAssembly();
        }

        return Path.GetDirectoryName(assembly.Location);
    }
}
