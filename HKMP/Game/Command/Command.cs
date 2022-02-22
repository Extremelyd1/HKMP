namespace Hkmp.Game.Command {
    /// <summary>
    /// Abstract base class for client and server-side commands.
    /// </summary>
    public abstract class Command {
        /// <summary>
        /// The trigger for this command, can include command prefix (such as "/").
        /// </summary>
        public abstract string Trigger { get; }

        /// <summary>
        /// Aliases for this command, can include command prefix (such as "/").
        /// </summary>
        public abstract string[] Aliases { get; }

        /// <summary>
        /// Executes the command with the given arguments.
        /// </summary>
        /// <param name="arguments">A string array containing the arguments for this command. The first argument
        /// is the command trigger or alias.</param>
        public abstract void Execute(string[] arguments);
    }
}