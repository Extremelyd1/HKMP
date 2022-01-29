namespace Hkmp.Api.Server {
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