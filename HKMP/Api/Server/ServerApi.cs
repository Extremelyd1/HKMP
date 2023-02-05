using Hkmp.Api.Command.Server;
using Hkmp.Api.Eventing;
using Hkmp.Api.Server.Networking;

namespace Hkmp.Api.Server;

/// <summary>
/// Server API interface implementation.
/// </summary>
internal class ServerApi : IServerApi {
    /// <inheritdoc/>
    public IServerManager ServerManager { get; }

    /// <inheritdoc/>
    public IServerCommandManager CommandManager { get; }

    /// <inheritdoc/>
    public INetServer NetServer { get; }

    /// <inheritdoc/>
    public IEventAggregator EventAggregator { get; }

    public ServerApi(
        IServerManager serverManager,
        IServerCommandManager commandManager,
        INetServer netServer,
        IEventAggregator eventAggregator
    ) {
        ServerManager = serverManager;
        CommandManager = commandManager;
        NetServer = netServer;
        EventAggregator = eventAggregator;
    }
}
