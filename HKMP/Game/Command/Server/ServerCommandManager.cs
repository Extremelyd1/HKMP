using Hkmp.Api.Command.Server;

namespace Hkmp.Game.Command.Server;

/// <summary>
/// Class that managed commands for server-side.
/// </summary>
internal class ServerCommandManager : CommandManager<IServerCommand>, IServerCommandManager {
    /// <summary>
    /// Try to process a command given the sender and the message.
    /// </summary>
    /// <param name="commandSender">The sender of the command.</param>
    /// <param name="message">A user-input string message.</param>
    /// <returns>True if the message was processed as a command, false otherwise.</returns>
    public bool ProcessCommand(ICommandSender commandSender, string message) {
        var arguments = GetArguments(message);

        if (arguments.Length == 0) {
            return false;
        }

        var commandString = arguments[0];
        if (Commands.TryGetValue(commandString, out var command)) {
            if (command.AuthorizedOnly && !commandSender.IsAuthorized) {
                commandSender.SendMessage("You are not authorized to execute this command");
                return true;
            }

            command.Execute(commandSender, arguments);

            return true;
        }

        return false;
    }
}
