using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hkmp.Api.Addon;

namespace Hkmp.Api.Client;

/// <summary>
/// Addon loader for the client-side.
/// </summary>
internal class ClientAddonLoader : AddonLoader {
    /// <summary>
    /// Loads all client addons.
    /// </summary>
    /// <returns>A list of ClientAddon instances.</returns>
    public List<ClientAddon> LoadAddons() {
        return LoadAddons<ClientAddon>();
    }

    /// <inheritdoc/>
    protected override string GetCurrentDirectoryPath() {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}
