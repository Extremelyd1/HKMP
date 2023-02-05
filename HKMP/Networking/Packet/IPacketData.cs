namespace Hkmp.Networking.Packet;

/// <summary>
/// An instance of data in a packet.
/// </summary>
public interface IPacketData {
    /// <summary>
    /// Whether the data contained in this class is considered reliable and requires resending if lost.
    /// </summary>
    bool IsReliable { get; }

    /// <summary>
    /// Whether lost reliable data in this class should be dropped if a newer version has already been
    /// received by the endpoint.
    /// </summary>
    bool DropReliableDataIfNewerExists { get; }

    /// <summary>
    /// Write the data in from the class into the given Packet instance.
    /// </summary>
    /// <param name="packet">The raw packet to write into.</param>
    void WriteData(IPacket packet);

    /// <summary>
    /// Read the data from the given Packet into the class.
    /// </summary>
    /// <param name="packet">The raw packet to read from.</param>
    void ReadData(IPacket packet);
}
