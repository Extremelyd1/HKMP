using Modding;

namespace Hkmp.Logging;

/// <summary>
/// Logger class for logging to the ModLog.txt file.
/// </summary>
internal class ModLogger : BaseLogger {
    /// <inheritdoc />
    public override void Info(string message) {
        Log(LogLevel.Info, $"[{GetOriginClassName()}] {message}");
    }

    /// <inheritdoc />
    public override void Fine(string message) {
        Log(LogLevel.Fine, $"[{GetOriginClassName()}] {message}");
    }

    /// <inheritdoc />
    public override void Debug(string message) {
        Log(LogLevel.Debug, $"[{GetOriginClassName()}] {message}");
    }

    /// <inheritdoc />
    public override void Warn(string message) {
        Log(LogLevel.Warn, $"[{GetOriginClassName()}] {message}");
    }

    /// <inheritdoc />
    public override void Error(string message) {
        Log(LogLevel.Error, $"[{GetOriginClassName()}] {message}");
    }

    /// <summary>
    /// Log the given message with the given log level to the ModLog.
    /// </summary>
    /// <param name="level">The log level of the message.</param>
    /// <param name="message">The message to log.</param>
    private static void Log(LogLevel level, string message) {
        Modding.Logger.Log(message, level);
    }
}
