using Hkmp.Api.Client.Networking;
using Hkmp.Api.Command.Client;
using Hkmp.Api.Eventing;

namespace Hkmp.Api.Client;

/// <summary>
/// The client API.
/// </summary>
public interface IClientApi {
    /// <summary>
    /// Client manager that handles the local client and related data.
    /// </summary>
    IClientManager ClientManager { get; }

    /// <summary>
    /// Command manager for registering client-side commands.
    /// </summary>
    IClientCommandManager CommandManager { get; }

    /// <summary>
    /// UI manager that handles all UI related interaction.
    /// </summary>
    IUiManager UiManager { get; }

    /// <summary>
    /// The net client for all network-related interaction.
    /// </summary>
    INetClient NetClient { get; }

    /// <summary>
    /// Inter-addon communication event bus.
    /// </summary>
    IEventAggregator EventAggregator { get; }
}
