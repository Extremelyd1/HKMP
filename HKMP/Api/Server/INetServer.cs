namespace Hkmp.Api.Server {
    public interface INetServer {
        
        /// <summary>
        /// Whether the server is currently started.
        /// </summary>
        bool IsStarted { get; }
        
    }
}