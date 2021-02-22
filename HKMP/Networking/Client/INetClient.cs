namespace HKMP.Networking.Client {
    public interface INetClient {

        void RegisterOnReceive(OnReceive onReceive);
        
        void Connect(string host, int port);

        void Disconnect();

        void Send(Packet.Packet packet);
    }
}