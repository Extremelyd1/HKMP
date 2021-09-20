namespace Hkmp.Api.Server {
    public interface INetServer {
        
        /**
         * Whether the server is currently started
         */
        bool IsStarted { get; }
        
    }
}