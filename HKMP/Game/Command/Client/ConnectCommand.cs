using Hkmp.Api.Command.Client;
using Hkmp.Game.Client;
using Hkmp.Ui;

namespace Hkmp.Game.Command.Client;

/// <summary>
/// Command for connecting the local user to a server with a given address, port and username.
/// </summary>
internal class ConnectCommand : IClientCommand {
    /// <inheritdoc />
    public string Trigger => "/connect";

    /// <inheritdoc />
    public string[] Aliases => new[] { "/disconnect" };

    /// <summary>
    /// The client manager instance.
    /// </summary>
    private readonly ClientManager _clientManager;

    public ConnectCommand(ClientManager clientManager) {
        _clientManager = clientManager;
    }

    /// <inheritdoc />
    public void Execute(string[] arguments) {
        var command = arguments[0];
        if (command == Aliases[0]) {
            _clientManager.Disconnect();
            UiManager.InternalChatBox.AddMessage("You are disconnected from the server");
            return;
        }

        if (arguments.Length != 4) {
            SendUsage();
            return;
        }

        var address = arguments[1];

        var portString = arguments[2];
        var parsedPort = int.TryParse(portString, out var port);
        if (!parsedPort || port < 1 || port > 99999) {
            UiManager.InternalChatBox.AddMessage("Invalid port!");
            return;
        }

        var username = arguments[3];

        _clientManager.Connect(address, port, username);
        UiManager.InternalChatBox.AddMessage($"Trying to connect to {address}:{port} as {username}...");
    }

    /// <summary>
    /// Sends the command usage to the chat box.
    /// </summary>
    private void SendUsage() {
        UiManager.InternalChatBox.AddMessage($"Invalid usage: {Trigger} <address> <port> <username>");
    }
}
