using System;

namespace Hkmp.Networking.Packet.Connection;

/// <summary>
/// Packet with raw byte data as a slice of a bigger chunk meant for large reliable data transfer during connection.
/// </summary>
internal class SlicePacket {
    /// <summary>
    /// The ID of the chunk that is being networked.
    /// </summary>
    public byte ChunkId { get; set; }

    /// <summary>
    /// The ID of this slice.
    /// </summary>
    public byte SliceId { get; set; }

    /// <summary>
    /// The total number of slices in this chunk + 1 (since we never use 0 slices in a chunk, we simply shift the
    /// values by one).
    /// </summary>
    public byte NumSlices { get; set; }

    /// <summary>
    /// Byte array containing the data of this slice.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Create a raw packet out of the data contained in this slice by writing to the given packet.
    /// </summary>
    /// <param name="packet">The packet instance to write the data to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the data cannot be written due to it being larger
    /// than the max slice size.</exception>
    public void CreatePacket(Packet packet) {
        packet.Write(ChunkId);
        packet.Write(SliceId);
        packet.Write(NumSlices);

        var length = Data.Length;
        if (length > ConnectionManager.MaxSliceSize) {
            throw new ArgumentOutOfRangeException(nameof(Data), "Length of data for slice cannot exceed 1024");
        }

        if (SliceId == NumSlices) {
            packet.Write((ushort) length);
        }

        for (var i = 0; i < length; i++) {
            packet.Write(Data[i]);
        }
    }

    /// <summary>
    /// Read the raw packet contents into this class.
    /// </summary>
    /// <param name="packet">The packet instance to read the data from.</param>
    /// <returns>False if the packet cannot be successfully read due to malformed data; otherwise true.</returns>
    public bool ReadPacket(Packet packet) {
        try {
            ChunkId = packet.ReadByte();
            SliceId = packet.ReadByte();
            NumSlices = packet.ReadByte();

            ushort length;
            if (SliceId == NumSlices) {
                length = packet.ReadUShort();
            } else {
                length = ConnectionManager.MaxSliceSize;
            }

            Data = new byte[length];
            for (var i = 0; i < length; i++) {
                Data[i] = packet.ReadByte();
            }
        } catch {
            return false;
        }

        return true;
    }
}
