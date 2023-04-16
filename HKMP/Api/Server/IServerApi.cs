using Hkmp.Api.Command.Server;
using Hkmp.Api.Eventing;
using Hkmp.Api.Server.Networking;

namespace Hkmp.Api.Server;

/// <summary>
/// The server API.
/// </summary>
public interface IServerApi {
    /// <summary>
    /// The interface for the server manager.
    /// </summary>
    IServerManager ServerManager { get; }

    /// <summary>
    /// Command manager for registering server-side commands.
    /// </summary>
    IServerCommandManager CommandManager { get; }

    /// <summary>
    /// The net server for all network-related interaction.
    /// </summary>
    INetServer NetServer { get; }

    /// <summary>
    /// Inter-addon communication event bus.
    /// </summary>
    IEventAggregator EventAggregator { get; }
}
