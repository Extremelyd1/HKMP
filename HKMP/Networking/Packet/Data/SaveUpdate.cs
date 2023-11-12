namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for when values in the save update.
/// </summary>
internal class SaveUpdate : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;
    
    /// <summary>
    /// The index of the save data entry that got updated.
    /// </summary>
    public ushort SaveDataIndex { get; set; }
    
    /// <summary>
    /// The encoded value of the save data in a byte array.
    /// </summary>
    public byte[] Value { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(SaveDataIndex);

        var length = (byte) System.Math.Min(Value.Length, byte.MaxValue);
        packet.Write(length);
        for (var i = 0; i < length; i++) {
            packet.Write(Value[i]);
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        SaveDataIndex = packet.ReadUShort();

        var length = packet.ReadByte();
        Value = new byte[length];
        for (var i = 0; i < length; i++) {
            Value[i] = packet.ReadByte();
        }
    }
}
