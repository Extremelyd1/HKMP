using System;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;

namespace Hkmp.Game.Command.Server;

/// <summary>
/// Command for changing the team of the player.
/// </summary>
internal class TeamCommand : IServerCommand {
    /// <inheritdoc />
    public string Trigger => "/team";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public bool AuthorizedOnly => false;

    /// <summary>
    /// The server manager instance.
    /// </summary>
    private readonly ServerManager _serverManager;

    public TeamCommand(ServerManager serverManager) {
        _serverManager = serverManager;
    }

    /// <inheritdoc />
    public void Execute(ICommandSender commandSender, string[] arguments) {
        if (commandSender.Type == CommandSenderType.Console) {
            commandSender.SendMessage("Console cannot change teams.");
            return;
        }

        var sender = (PlayerCommandSender) commandSender;

        if (arguments.Length != 2) {
            sender.SendMessage($"Usage: {Trigger} <None|Moss|Hive|Grimm|Lifeblood>");
            return;
        }

        var teamName = arguments[1];
        if (!Enum.TryParse<Team>(teamName, true, out var team)) {
            sender.SendMessage($"Unknown team name: '{teamName}'");
            return;
        }

        if (_serverManager.TryUpdatePlayerTeam(sender.Id, team, out var reason)) {
            sender.SendMessage($"Team changed to '{team}'");
        } else {
            sender.SendMessage(reason);
        }
    }
}
