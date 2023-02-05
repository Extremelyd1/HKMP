namespace Hkmp.Logging;

/// <summary>
/// Logger that logs messages to the output.
/// </summary>
public interface ILogger {
    /// <summary>
    /// Log a message as information.
    /// </summary>
    /// <param name="message">The string message.</param>
    void Info(string message);

    /// <summary>
    /// Log a message as fine information.
    /// </summary>
    /// <param name="message">The string message.</param>
    void Fine(string message);

    /// <summary>
    /// Log a message as debug information.
    /// </summary>
    /// <param name="message">The string message.</param>
    void Debug(string message);

    /// <summary>
    /// Log a message as a warning.
    /// </summary>
    /// <param name="message">The string message.</param>
    void Warn(string message);

    /// <summary>
    /// Log a message as an error.
    /// </summary>
    /// <param name="message">The string message.</param>
    void Error(string message);
}
