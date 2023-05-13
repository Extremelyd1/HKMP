using System;
using Hkmp.Logging;

namespace Hkmp.Api.Client;

/// <summary>
/// Abstract base class for a client addon. Inheriting this will allow the addon class to be loaded.
/// </summary>
public abstract class ClientAddon : Addon.Addon {
    /// <summary>
    /// The client API interface.
    /// </summary>
    private IClientApi _clientApi;

    /// <inheritdoc cref="_clientApi" />
    protected IClientApi ClientApi {
        get {
            if (this is TogglableClientAddon { Disabled: true }) {
                throw new InvalidOperationException("Addon is disabled, cannot use Client API in this state");
            }

            return _clientApi;
        }
    }

    /// <summary>
    /// The logger for logging information.
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
    /// <param name="clientApi">The client API instance.</param>
    internal void InternalInitialize(IClientApi clientApi) {
        _clientApi = clientApi;

        Initialize(clientApi);
    }

    /// <summary>
    /// Called when the addon is loaded and can be initialized.
    /// </summary>
    /// <param name="clientApi">The client API interface.</param>
    public abstract void Initialize(IClientApi clientApi);

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
    /// Register a client addon to be initialized and managed by HKMP.
    /// This method can only be called during the initialization of mods.
    /// After all mods have been initialized, this will throw an exception.
    /// </summary>
    /// <param name="clientAddon">The client addon to be registered.</param>
    /// <exception cref="ArgumentException">Thrown if the given addon is null.</exception>
    public static void RegisterAddon(ClientAddon clientAddon) {
        if (clientAddon == null) {
            throw new ArgumentException("Client addon can not be null");
        }

        ClientAddonManager.RegisterAddon(clientAddon);
    }
}
