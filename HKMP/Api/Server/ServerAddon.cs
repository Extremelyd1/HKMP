using System;
using Hkmp.Logging;

namespace Hkmp.Api.Server;

/// <summary>
/// Abstract base class for a server addon. Inheriting this will allow the addon class to be loaded.
/// </summary>
public abstract class ServerAddon : Addon.Addon {
    /// <summary>
    /// The server API interface.
    /// </summary>
    protected IServerApi ServerApi { get; private set; }

    /// <summary>
    /// The logger for logger information.
    /// </summary>
    protected ILogger Logger => AddonLogger.Instance;

    /// <summary>
    /// The name (and also identifier) of the addon.
    /// </summary>
    protected abstract string Name { get; }

    /// <summary>
    /// The version (also identifying) of the addon.
    /// </summary>
    protected abstract string Version { get; }

    /// <summary>
    /// Whether this addon requires network access.
    /// </summary>
    public abstract bool NeedsNetwork { get; }

    /// <summary>
    /// Internal method for initializing the addon with the API.
    /// </summary>
    /// <param name="serverApi">The server API instance.</param>
    internal void InternalInitialize(IServerApi serverApi) {
        ServerApi = serverApi;

        Initialize(serverApi);
    }

    /// <summary>
    /// Called when the addon is loaded and can be initialized.
    /// </summary>
    /// <param name="serverApi">The server API interface.</param>
    public abstract void Initialize(IServerApi serverApi);

    /// <summary>
    /// Internal method for obtaining the length-valid addon name.
    /// </summary>
    /// <returns>The name of the addon or a substring of the first valid characters of its name.</returns>
    internal string GetName() {
        if (Name.Length > MaxNameLength) {
            return Name.Substring(0, MaxNameLength);
        }

        return Name;
    }

    /// <summary>
    /// Internal method for obtaining the length-valid addon version.
    /// </summary>
    /// <returns>The version of the addon or a substring of the first valid characters of its version.</returns>
    internal string GetVersion() {
        if (Version.Length > MaxVersionLength) {
            return Version.Substring(0, MaxVersionLength);
        }

        return Version;
    }

    /// <summary>
    /// Register a server addon to be initialized and managed by HKMP.
    /// This method can only be called during the initialization of mods.
    /// After all mods have been initialized, this will throw an exception.
    /// </summary>
    /// <param name="serverAddon">The server addon to be registered.</param>
    /// <exception cref="ArgumentException">Thrown if the given addon is null.</exception>
    public static void RegisterAddon(ServerAddon serverAddon) {
        if (serverAddon == null) {
            throw new ArgumentException("Server addon can not be null");
        }

        ServerAddonManager.RegisterAddon(serverAddon);
    }
}
