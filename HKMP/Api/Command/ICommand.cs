namespace Hkmp.Api.Command {
    /// <summary>
    /// Interface for client and server-side commands.
    /// </summary>
    public interface ICommand {
        /// <summary>
        /// The trigger for this command, can include command prefix (such as "/").
        /// </summary>
        string Trigger { get; }

        /// <summary>
        /// Aliases for this command, can include command prefix (such as "/").
        /// </summary>
        string[] Aliases { get; }

        /// <summary>
        /// Executes the command with the given arguments.
        /// </summary>
        /// <param name="arguments">A string array containing the arguments for this command. The first argument
        /// is the command trigger or alias.</param>
        void Execute(string[] arguments);
    }
}