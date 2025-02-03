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
    /// The total number of slices in this chunk + 1 (since we never use 0 slices in a chunk, we simply shift the
    /// values by one).
    /// </summary>
    public byte NumSlices { get; set; }

    /// <summary>
    /// Byte array containing the data of this slice.
    /// </summary>
    public byte[] Data { get; set; }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
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

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
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
    }
}
