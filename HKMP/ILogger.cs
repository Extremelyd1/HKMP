namespace Hkmp {
    /// <summary>
    /// Logger that logs messages to the output.
    /// </summary>
    public interface ILogger {
        /// <summary>
        /// Log a message from the given origin object as information.
        /// </summary>
        /// <param name="origin">The origin object of the message.</param>
        /// <param name="message">The string message.</param>
        void Info(object origin, string message);

        /// <summary>
        /// Log a message from the given origin object as fine information.
        /// </summary>
        /// <param name="origin">The origin object of the message.</param>
        /// <param name="message">The string message.</param>
        void Fine(object origin, string message);

        /// <summary>
        /// Log a message from the given origin object as debug information.
        /// </summary>
        /// <param name="origin">The origin object of the message.</param>
        /// <param name="message">The string message.</param>
        void Debug(object origin, string message);

        /// <summary>
        /// Log a message from the given origin object as a warning.
        /// </summary>
        /// <param name="origin">The origin object of the message.</param>
        /// <param name="message">The string message.</param>
        void Warn(object origin, string message);

        /// <summary>
        /// Log a message from the given origin object as an error.
        /// </summary>
        /// <param name="origin">The origin object of the message.</param>
        /// <param name="message">The string message.</param>
        void Error(object origin, string message);
    }
}