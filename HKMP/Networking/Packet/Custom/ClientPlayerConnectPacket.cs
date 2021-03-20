namespace HKMP.Networking.Packet.Custom {
    public class ClientPlayerConnectPacket : Packet, IPacket {

        public ushort Id { get; set; }
        public string Username { get; set; }

        public ClientPlayerConnectPacket() {
        }

        public ClientPlayerConnectPacket(Packet packet) : base(packet) {
        }

        public Packet CreatePacket() {
            Reset();
            
            Write(PacketId.PlayerConnect);

            Write(Id);
            Write(Username);

            WriteLength();

            return this;
        }

        public void ReadPacket() {
            Id = ReadUShort();
            Username = ReadString();
        }
    }
}