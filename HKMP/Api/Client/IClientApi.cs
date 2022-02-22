using Hkmp.Api.Command;
using JetBrains.Annotations;

namespace Hkmp.Api.Client {
    /// <summary>
    /// The client API.
    /// </summary>
    [PublicAPI]
    public interface IClientApi {
        /// <summary>
        /// Client manager that handles the local client and related data.
        /// </summary>
        IClientManager ClientManager { get; }

        /// <summary>
        /// Command manager for registering client-side commands.
        /// </summary>
        ICommandManager CommandManager { get; }

        /// <summary>
        /// UI manager that handles all UI related interaction.
        /// </summary>
        IUiManager UiManager { get; }

        /// <summary>
        /// The net client for all network-related interaction.
        /// </summary>
        INetClient NetClient { get; }
    }
}