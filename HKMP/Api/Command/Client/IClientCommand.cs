
namespace Hkmp.Api.Command.Client;

/// <summary>
/// Interface for client-side commands.
/// </summary>
public interface IClientCommand : ICommand {
    /// <summary>
    /// Executes the command with the given arguments.
    /// </summary>
    /// <param name="arguments">A string array containing the arguments for this command. The first argument
    /// is the command trigger or alias.</param>
    void Execute(string[] arguments);
}
