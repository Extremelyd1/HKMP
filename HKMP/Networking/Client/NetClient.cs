using UnityEngine;

namespace HKMP.Networking.Client {
    public delegate bool OnReceive(byte[] receivedData);

    public abstract class NetClient : INetClient {
        protected static readonly int MaxBufferSize = (int) Mathf.Pow(2, 20);
        
        protected OnReceive onReceive;

        public NetClient() {
        }

        public void RegisterOnReceive(OnReceive onReceive) {
            this.onReceive = onReceive;
        }

        public abstract void Connect(string host, int port);

        public abstract void Disconnect();

        public abstract void Send(Packet.Packet packet);
    }
}