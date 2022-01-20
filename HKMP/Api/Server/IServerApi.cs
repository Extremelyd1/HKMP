namespace Hkmp.Api.Server {
    public interface IServerApi {
        /// <summary>
        /// TODO: figure out documentation once interface is more finished
        /// </summary>
        /// <returns></returns>
        IServerManager ServerManager { get; }

        /// <summary>
        /// The net server for all network-related interaction.
        /// </summary>
        INetServer NetServer { get; }
    }
}