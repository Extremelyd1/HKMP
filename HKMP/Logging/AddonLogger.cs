namespace Hkmp.Logging;

/// <summary>
/// ILogger wrapper that forwards to the static <see cref="Logger"/> methods.
/// </summary>
internal class AddonLogger : ILogger {
    /// <summary>
    /// The static addon logger instance.
    /// </summary>
    private static AddonLogger _instance;

    /// <summary>
    /// Static addon logger property that accesses/creates the instance.
    /// </summary>
    public static AddonLogger Instance => _instance ??= new AddonLogger();

    /// <inheritdoc />
    public void Info(string message) {
        Logger.Info(message);
    }

    /// <inheritdoc />
    public void Fine(string message) {
        Logger.Fine(message);
    }

    /// <inheritdoc />
    public void Debug(string message) {
        Logger.Debug(message);
    }

    /// <inheritdoc />
    public void Warn(string message) {
        Logger.Warn(message);
    }

    /// <inheritdoc />
    public void Error(string message) {
        Logger.Error(message);
    }
}
