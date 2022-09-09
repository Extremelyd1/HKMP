using System;
using System.Diagnostics;

namespace Hkmp.Logging;

/// <summary>
/// Abstract base class for loggers that prepends messages with their log level and origin class.
/// </summary>
public abstract class BaseLogger : ILogger {
    /// <summary>
    /// Get the class name of the object that called the log function in which this method is used. Will skip
    /// classes in the stack frame that are within the "Hkmp.Logging" namespace.
    /// Note that this method is prone to breaking if namespace changes or stack frame changes for logging occur.
    /// </summary>
    /// <returns>The full class name of the origin object or name of the method if no such object exists.</returns>
    protected static string GetOriginClassName() {
        string typeString;
        Type declaringType;
        var skipFrames = 3;

        do {
            var methodBase = new StackFrame(skipFrames, false).GetMethod();

            declaringType = methodBase.DeclaringType;
            if (declaringType == null) {
                return methodBase.Name;
            }

            skipFrames++;
            typeString = declaringType.ToString();
        } while (
            declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase)
            || (declaringType.Namespace != null && declaringType.Namespace.StartsWith("Hkmp.Logging"))
        );

        return typeString;
    }

    /// <inheritdoc />
    public abstract void Info(string message);

    /// <inheritdoc />
    public abstract void Fine(string message);

    /// <inheritdoc />
    public abstract void Debug(string message);

    /// <inheritdoc />
    public abstract void Warn(string message);

    /// <inheritdoc />
    public abstract void Error(string message);
}
