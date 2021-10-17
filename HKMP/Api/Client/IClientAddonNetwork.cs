using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    public interface IClientAddonNetwork {

        void SendData(IPacketData packetData);

    }
}