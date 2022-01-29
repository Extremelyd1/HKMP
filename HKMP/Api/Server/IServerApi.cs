using JetBrains.Annotations;

namespace Hkmp.Api.Server {
    /// <summary>
    /// The server API.
    /// </summary>
    [PublicAPI]
    public interface IServerApi {
        /// <summary>
        /// The interface for the server manager.
        /// </summary>
        IServerManager ServerManager { get; }

        /// <summary>
        /// The net server for all network-related interaction.
        /// </summary>
        INetServer NetServer { get; }
    }
}