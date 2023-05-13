namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data from server to client that another player has disconnected.
/// </summary>
internal class ClientPlayerDisconnect : GenericClientData {
    /// <summary>
    /// The username of the player that disconnected.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Whether the player receiving this data becomes the new scene host.
    /// </summary>
    public bool NewSceneHost { get; set; }
    
    /// <summary>
    /// Whether the player timed out or disconnected normally.
    /// </summary>
    public bool TimedOut { get; set; }

    /// <summary>
    /// Construct the client player disconnect data.
    /// </summary>
    public ClientPlayerDisconnect() {
        IsReliable = true;
        DropReliableDataIfNewerExists = false;
    }

    /// <inheritdoc />
    public override void WriteData(IPacket packet) {
        packet.Write(Id);
        packet.Write(Username);
        packet.Write(NewSceneHost);
        packet.Write(TimedOut);
    }

    /// <inheritdoc />
    public override void ReadData(IPacket packet) {
        Id = packet.ReadUShort();
        Username = packet.ReadString();
        NewSceneHost = packet.ReadBool();
        TimedOut = packet.ReadBool();
    }
}
