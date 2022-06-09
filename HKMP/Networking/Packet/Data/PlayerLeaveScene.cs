namespace Hkmp.Networking.Packet.Data {
    /// <summary>
    /// Packet data for the client-bound player leave scene data.
    /// </summary>
    internal class ClientPlayerLeaveScene : GenericClientData {
        /// <summary>
        /// Whether the player receiving this data becomes the new scene host.
        /// </summary>
        public bool NewSceneHost { get; set; }

        /// <inheritdoc />
        public override void WriteData(IPacket packet) {
            packet.Write(Id);
            packet.Write(NewSceneHost);
        }

        /// <inheritdoc />
        public override void ReadData(IPacket packet) {
            Id = packet.ReadUShort();
            NewSceneHost = packet.ReadBool();
        }
    }
}