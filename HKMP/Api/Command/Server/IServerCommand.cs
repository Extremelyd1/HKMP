
namespace Hkmp.Api.Command.Server;

/// <summary>
/// Interface for server-side commands.
/// </summary>
public interface IServerCommand : ICommand {
    /// <summary>
    /// Whether this command can only be executed by an authorized player.
    /// </summary>
    bool AuthorizedOnly { get; }

    /// <summary>
    /// Executes the command with the given arguments.
    /// </summary>
    /// <param name="commandSender">The command sender that executed this command.</param>
    /// <param name="arguments">A string array containing the arguments for this command. The first argument
    /// is the command trigger or alias.</param>
    void Execute(ICommandSender commandSender, string[] arguments);
}
