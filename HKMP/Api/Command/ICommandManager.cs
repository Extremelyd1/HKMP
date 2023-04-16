using System;

namespace Hkmp.Api.Command;

/// <summary>
/// Interface for managing commands for client and server-side.
/// </summary>
public interface ICommandManager<in TCommand> {
    /// <summary>
    /// Register a given command with at least its trigger. Aliases may or may not be registered depending on
    /// whether they are available.
    /// </summary>
    /// <param name="clientCommand">The command implementation.</param>
    /// <exception cref="Exception">Thrown if a command with that trigger is already registered.</exception>
    void RegisterCommand(TCommand clientCommand);

    /// <summary>
    /// Deregister a given command with at least its trigger. Aliases will be de-registered if they were registered
    /// for this command.
    /// </summary>
    /// <param name="clientCommand">The command implementation.</param>
    /// <exception cref="Exception">Thrown if the trigger of the command was registered by a different command.</exception>
    void DeregisterCommand(TCommand clientCommand);
}
