namespace HKMP.Networking.Server {
    public interface INetServer {

        void Start(int port);

        void Stop();
    }
}