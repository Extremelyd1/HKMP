using JetBrains.Annotations;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Abstract base class for a client addon. Inheriting this will allow the addon class to be loaded.
    /// </summary>
    [PublicAPI]
    public abstract class ClientAddon : Addon.Addon {
        
        /// <summary>
        /// The client API interface.
        /// </summary>
        protected IClientApi ClientApi { get; }

        /// <summary>
        /// The logger for logging information.
        /// </summary>
        protected ILogger Logger => Hkmp.Logger.Get();

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
        /// Called when the addon is loaded and can be initialized.
        /// </summary>
        public abstract void Initialize();

        public ClientAddon(IClientApi clientApi) {
            ClientApi = clientApi;
        }
        
        /// <summary>
        /// Internal method for obtaining the length-valid addon name.
        /// </summary>
        /// <returns>The name of the addon or a substring of the first valid characters of its name.</returns>
        public string GetName() {
            if (Name.Length > MaxNameLength) {
                return Name.Substring(0, MaxNameLength);
            }

            return Name;
        }
        
        /// <summary>
        /// Internal method for obtaining the length-valid addon version.
        /// </summary>
        /// <returns>The version of the addon or a substring of the first valid characters of its version.</returns>
        public string GetVersion() {
            if (Version.Length > MaxVersionLength) {
                return Version.Substring(0, MaxVersionLength);
            }

            return Version;
        }
    }
}