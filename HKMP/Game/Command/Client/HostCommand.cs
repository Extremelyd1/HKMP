using System;
using Hkmp.Api.Command.Client;
using Hkmp.Game.Server;
using Hkmp.Ui;

namespace Hkmp.Game.Command.Client;

/// <summary>
/// Command for controlling local server hosting.
/// </summary>
internal class HostCommand : IClientCommand {
    /// <inheritdoc />
    public string Trigger => "/host";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <summary>
    /// The server manager instance.
    /// </summary>
    private readonly ServerManager _serverManager;

    public HostCommand(ServerManager serverManager) {
        _serverManager = serverManager;
    }

    /// <inheritdoc />
    public void Execute(string[] arguments) {
        if (arguments.Length < 2) {
            SendUsage();
            return;
        }

        var action = arguments[1];
        if (action == "start") {
            if (arguments.Length != 3) {
                SendUsage();
                return;
            }

            var portString = arguments[2];
            var parsedPort = int.TryParse(portString, out var port);
            if (!parsedPort || port < 1 || port > 99999) {
                UiManager.InternalChatBox.AddMessage("Invalid port!");
                return;
            }

            _serverManager.Start(port);
            UiManager.InternalChatBox.AddMessage($"Started server on port {port}");
        } else if (action == "stop") {
            _serverManager.Stop();
            UiManager.InternalChatBox.AddMessage("Stopped server");
        } else {
            SendUsage();
        }
    }

    /// <summary>
    /// Sends the command usage to the chat box.
    /// </summary>
    private void SendUsage() {
        UiManager.InternalChatBox.AddMessage($"Invalid usage: {Trigger} <start|stop> [port]");
    }
}
