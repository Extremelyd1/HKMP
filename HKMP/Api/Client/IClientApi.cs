using JetBrains.Annotations;

namespace Hkmp.Api.Client {
    /// <summary>
    /// The client API.
    /// </summary>
    [PublicAPI]
    public interface IClientApi {
        /// <summary>
        /// TODO: figure out documentation once interface is more finished
        /// </summary>
        IClientManager ClientManager { get; }

        /// <summary>
        /// The net client for all network-related interaction.
        /// </summary>
        INetClient NetClient { get; }
    }
}