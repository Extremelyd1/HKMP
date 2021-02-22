namespace HKMP.Networking.Server {
    public abstract class NetServer : INetServer {
        
        
        public abstract void Start(int port);

        public abstract void Stop();
    }
}