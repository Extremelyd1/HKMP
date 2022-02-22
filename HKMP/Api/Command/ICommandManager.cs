using System;

namespace Hkmp.Api.Command {
    /// <summary>
    /// Interface for managing commands for both client and server side.
    /// </summary>
    public interface ICommandManager {
        /// <summary>
        /// Register a given command with at least its trigger. Aliases may or may not be registered depending on
        /// whether they are available.
        /// </summary>
        /// <param name="command">The command implementation.</param>
        /// <exception cref="Exception">Thrown if a command with that trigger is already registered.</exception>
        void RegisterCommand(ICommand command);

        /// <summary>
        /// Deregister a given command with at least its trigger. Aliases will be de-registered if they were registered
        /// for this command.
        /// </summary>
        /// <param name="command">The command implementation.</param>
        /// <exception cref="Exception">Thrown if the trigger of the command was registered by a different command.</exception>
        void DeregisterCommand(ICommand command);
    }
}