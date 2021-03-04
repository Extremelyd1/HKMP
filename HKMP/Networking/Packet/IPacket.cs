namespace HKMP.Networking.Packet {
    public interface IPacket {

        Packet CreatePacket();

        void ReadPacket();

    }
}