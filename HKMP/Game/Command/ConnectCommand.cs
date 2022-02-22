using System;
using Hkmp.Game.Client;
using Hkmp.Ui;

namespace Hkmp.Game.Command {
    public class ConnectCommand : Command {
        public override string Trigger => "/connect";
        public override string[] Aliases => new[] { "/disconnect" };

        private readonly ClientManager _clientManager;

        public ConnectCommand(ClientManager clientManager) {
            _clientManager = clientManager;
        }

        public override void Execute(string[] arguments) {
            var command = arguments[0];
            if (command == Aliases[0]) {
                _clientManager.Disconnect();
                UiManager.InternalChatBox.AddMessage("Disconnected from server");
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

        private void SendUsage() {
            UiManager.InternalChatBox.AddMessage($"Invalid usage: {Trigger} <address> <port> <username>");
        }
    }
}