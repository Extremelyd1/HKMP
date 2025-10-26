using System;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;

namespace Hkmp.Game.Command.Server;

/// <summary>
/// Command for changing the skin of the player.
/// </summary>
internal class SkinCommand : IServerCommand {
    /// <inheritdoc />
    public string Trigger => "/skin";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public bool AuthorizedOnly => false;

    /// <summary>
    /// The server manager instance.
    /// </summary>
    private readonly ServerManager _serverManager;

    public SkinCommand(ServerManager serverManager) {
        _serverManager = serverManager;
    }

    /// <inheritdoc />
    public void Execute(ICommandSender commandSender, string[] arguments) {
        if (commandSender.Type == CommandSenderType.Console) {
            commandSender.SendMessage("Console cannot change skins.");
            return;
        }

        var sender = (PlayerCommandSender) commandSender;

        if (arguments.Length != 2) {
            sender.SendMessage($"Usage: {Trigger} <skin ID>");
            return;
        }

        var skinIdArg = arguments[1];
        if (!byte.TryParse(skinIdArg, out var skinId)) {
            sender.SendMessage($"Unknown skin ID '{skinIdArg}', please provide a value between 0-255");
            return;
        }

        if (_serverManager.TryUpdatePlayerSkin(sender.Id, skinId, out var reason)) {
            sender.SendMessage($"Skin ID changed to '{skinId}'");
        } else {
            sender.SendMessage(reason);
        }
    }
}
