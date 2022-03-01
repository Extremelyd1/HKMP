namespace Hkmp.Api.Command.Server {
    /// <summary>
    /// Interface for an entity that can execute commands.
    /// </summary>
    public interface ICommandSender {
        /// <summary>
        /// Whether this user is authorized, meaning they have high-level permission.
        /// </summary>
        bool IsAuthorized { get; }

        /// <summary>
        /// Send a message to this command sender.
        /// </summary>
        /// <param name="message">The message in string form.</param>
        void SendMessage(string message);
    }
}