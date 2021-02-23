using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using HKMP.Networking.Server;

namespace HKMP.Game {
    public class GameManager {
        private const string Localhost = "127.0.0.1";
        private const string Ip = "192.168.2.2";
        private const int Port = 26950;

        private NetServer _netServer;
        private NetClient _netClient;

        public GameManager() {
            CreateClient();
        }

        private void CreateClient() {
            Logger.Info(this, "Creating instances for client");
        
            var packetManager = new PacketManager();
            packetManager.RegisterClientPacketHandler(PacketId.HelloClient, HandleHelloClient);
            packetManager.RegisterClientPacketHandler(PacketId.Test1, HandleTest1);

            _netClient = new NetClient(packetManager);
            _netClient.Connect(Ip, Port);

            _netClient.RegisterOnConnect(() => {
                Logger.Info(this, "NetClient was connected");
                
                var packet = new Packet(PacketId.HelloServer);
                packet.Write("[Client] Server-bound packet");
                _netClient.SendTcp(packet);
            });
        }

        private void CreateHost() {
            Logger.Info(this, "Creating instances for host");
            
            var packetManager = new PacketManager();
            packetManager.RegisterClientPacketHandler(PacketId.HelloClient, HandleHelloClient);
            packetManager.RegisterClientPacketHandler(PacketId.Test1, HandleTest1);
            
            packetManager.RegisterServerPacketHandler(PacketId.HelloServer, HandleHelloServer);
            packetManager.RegisterServerPacketHandler(PacketId.Test2, HandleTest2);
            
            _netServer = new NetServer(packetManager);
            
            _netServer.Start(Port);
            
            _netClient = new NetClient(packetManager);
            
            _netClient.Connect(Localhost, Port);
            
            _netClient.RegisterOnConnect(() => {
                Logger.Info(this, "NetClient was connected");
                
                var packet = new Packet(PacketId.HelloServer);
                packet.Write("[Host] Server-bound packet");
                _netClient.SendTcp(packet);
            });
        }

        private void HandleHelloClient(Packet packet) {
            Logger.Info(this, "Received Hello Client packet");

            var stringValue = packet.ReadString();
            Logger.Info(this, $"  {stringValue}");

            packet = new Packet(PacketId.Test2);
            packet.Write("[Client] Server-bound UDP packet");

            _netClient.SendUdp(packet);
        }

        private void HandleHelloServer(int id, Packet packet) {
            Logger.Info(this, "Received Hello Server packet");
            
            var stringValue = packet.ReadString();
            Logger.Info(this, $"  {stringValue}");

            packet = new Packet(PacketId.HelloClient);
            packet.Write("[Host] Client-bound packet");
            _netServer.SendTcp(id, packet);
        }
        
        private void HandleTest1(Packet packet) {
            Logger.Info(this, "Received Test1 packet");
            
            var stringValue = packet.ReadString();
            Logger.Info(this, $"  {stringValue}");
        }
        
        private void HandleTest2(int id, Packet packet) {
            Logger.Info(this, "Received Test2 packet");
            
            var stringValue = packet.ReadString();
            Logger.Info(this, $"  {stringValue}");
            
            packet = new Packet(PacketId.Test1);
            packet.Write("[Host] Client-bound UDP packet");
            _netServer.SendUdp(id, packet);
        }
        
    }
}