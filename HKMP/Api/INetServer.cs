namespace Hkmp.Api {
    public interface INetServer {
        
        /**
         * Whether the server is currently started
         */
        bool IsStarted { get; }
        
    }
}