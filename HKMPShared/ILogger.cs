namespace Hkmp {
    public interface ILogger {
        void Info(object origin, string message);

        void Fine(object origin, string message);

        void Debug(object origin, string message);

        void Warn(object origin, string message);

        void Error(object origin, string message);
    }
}