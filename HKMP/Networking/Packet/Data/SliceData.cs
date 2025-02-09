using System;

namespace Hkmp.Networking.Packet.Data;

/// <summary>
/// Packet with raw byte data as a slice of a bigger chunk meant for large reliable data transfer during connection.
/// </summary>
internal class SliceData : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => false;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;
    
    /// <summary>
    /// The ID of the chunk that is being networked.
    /// </summary>
    public byte ChunkId { get; set; }

    /// <summary>
    /// The ID of this slice.
    /// </summary>
    public byte SliceId { get; set; }

    /// <summary>
    /// The total number of slices in this chunk. It is an unsigned short because we can have 256 slices in a chunk.
    /// It is encoded as a byte, where all values are shifted by one since 0 is not used.
    /// </summary>
    public ushort NumSlices { get; set; }

    /// <summary>
    /// Byte array containing the data of this slice.
    /// </summary>
    public byte[] Data { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        packet.Write(ChunkId);
        packet.Write(SliceId);

        // Shift all values by -1 so that we can encode 256 as a number of slices
        var encodedNumSlices = (byte) (NumSlices - 1);
        packet.Write(encodedNumSlices);

        var length = Data.Length;
        if (length > ConnectionManager.MaxSliceSize) {
            throw new ArgumentOutOfRangeException(nameof(Data), "Length of data for slice cannot exceed 1024");
        }

        if (SliceId == NumSlices - 1) {
            packet.Write((ushort) length);
        }

        for (var i = 0; i < length; i++) {
            packet.Write(Data[i]);
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        ChunkId = packet.ReadByte();
        SliceId = packet.ReadByte();

        // Read the encoded byte and shift it by 1 again
        var encodedNumSlices = packet.ReadByte();
        NumSlices = (ushort) (encodedNumSlices + 1);

        ushort length;
        if (SliceId == NumSlices - 1) {
            length = packet.ReadUShort();
        } else {
            length = ConnectionManager.MaxSliceSize;
        }

        Data = new byte[length];
        for (var i = 0; i < length; i++) {
            Data[i] = packet.ReadByte();
        }
    }
}
