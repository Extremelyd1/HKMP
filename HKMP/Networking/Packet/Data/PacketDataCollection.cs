namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet data for a collection of individual packet data instances.
/// </summary>
/// <typeparam name="T">The type of the underlying packet data instances.</typeparam>
public class PacketDataCollection<T> : RawPacketDataCollection, IPacketData where T : IPacketData, new() {
    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        var length = (ushort) System.Math.Min(ushort.MaxValue, DataInstances.Count);

        packet.Write(length);

        for (var i = 0; i < length; i++) {
            DataInstances[i].WriteData(packet);
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        var length = packet.ReadUShort();

        for (var i = 0; i < length; i++) {
            // Create new instance of generic type
            var instance = new T();

            // Read the packet data into the instance
            instance.ReadData(packet);

            // And add it to our already initialized list
            DataInstances.Add(instance);
        }
    }
}
