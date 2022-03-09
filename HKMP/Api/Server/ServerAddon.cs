using JetBrains.Annotations;

namespace Hkmp.Api.Server {
    /// <summary>
    /// Abstract base class for a server addon. Inheriting this will allow the addon class to be loaded.
    /// </summary>
    [PublicAPI]
    public abstract class ServerAddon : Addon.Addon {
        /// <summary>
        /// The server API interface.
        /// </summary>
        protected IServerApi ServerApi { get; }

        /// <summary>
        /// The logger for logger information.
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

        /// <summary>
        /// Constructs the server addon with the given server API.
        /// </summary>
        /// <param name="serverApi">The server API interface.</param>
        protected ServerAddon(IServerApi serverApi) {
            ServerApi = serverApi;
        }

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
    }
}