namespace Hkmp.Networking.Packet.Connection;

/// <summary>
/// Packet for acknowledging a received slice packet for large reliable data transfer during connection.
/// </summary>
internal class SliceAckPacket {
    /// <summary>
    /// The ID of the chunk that is being networked.
    /// </summary>
    public byte ChunkId { get; set; }

    /// <summary>
    /// The total number of slices in this chunk - 1 (since we never use 0 slices in a chunk, we simply shift the
    /// values by one, e.g. this value is 0 for 1 slice, 5 for 6 slices, etc.).
    /// </summary>
    public byte NumSlicesMinusOne { get; set; }

    /// <summary>
    /// Boolean array containing whether a slice was acked. For writing packets, the length of the array can equal
    /// the number of slices. For reading packets, the length of the array will equal the maximum possible number
    /// of slices per chunk.
    /// </summary>
    public bool[] Acked { get; set; }

    /// <summary>
    /// Create a raw packet out of the data contained in this acknowledgement to the given packet.
    /// </summary>
    /// <param name="packet">The packet instance to write the data to.</param>
    public void CreatePacket(Packet packet) {
        packet.Write(ChunkId);
        packet.Write(NumSlicesMinusOne);

        // Keep track of current index for writing ack array
        var currentIndex = 0;
        // Do while loop, since we will always be writing at least a single byte bit flag
        do {
            packet.Write(CreateAckFlag(currentIndex, currentIndex + 8, Acked));
            // Continue while loop if we need to write another flag, namely when the new starting index is smaller
            // than the number of slices
        } while ((currentIndex += 8) <= NumSlicesMinusOne);
    }

    /// <summary>
    /// Read the raw packet contents into this class.
    /// </summary>
    /// <param name="packet">The packet instance to read the data from.</param>
    /// <returns>False if the packet cannot be successfully read due to malformed data; otherwise true.</returns>
    public bool ReadPacket(Packet packet) {
        try {
            ChunkId = packet.ReadByte();
            NumSlicesMinusOne = packet.ReadByte();

            var acked = new bool[ConnectionManager.MaxSlicesPerChunk];

            // Keep track of current index for writing to ack array
            var currentIndex = 0;
            // Do while loop, since we will always be reading at least one byte for the bit flag
            do {
                var flag = packet.ReadByte();
                ReadAckFlag(flag, currentIndex, currentIndex + 8, ref acked);
                // Continue while loop if we need to read another flag, namely when the new starting index is smaller
                // than the number of slices
            } while ((currentIndex += 8) <= NumSlicesMinusOne);

            Acked = acked;
        } catch {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Create a bit flag as a byte from the given boolean array with start and end indices.
    /// </summary>
    /// <param name="startIndex">The (inclusive) start index to start reading from the boolean array.</param>
    /// <param name="endIndex">The (exclusive) end index to stop reading from the boolean array.</param>
    /// <param name="acked">The boolean array to read values from for the flag.</param>
    /// <returns>The bit flag as a byte.</returns>
    private static byte CreateAckFlag(int startIndex, int endIndex, bool[] acked) {
        byte flag = 0;
        byte currentValue = 1;

        for (var i = startIndex; i < endIndex; i++) {
            if (acked.Length <= i) {
                break;
            }
            
            if (acked[i]) {
                flag |= currentValue;
            }

            currentValue *= 2;
        }

        return flag;
    }

    /// <summary>
    /// Read a bit flag in byte form and put the bits into the given reference boolean array.
    /// </summary>
    /// <param name="flag">The bit flag as a byte.</param>
    /// <param name="startIndex">The (inclusive) start index to start reading from the boolean array.</param>
    /// <param name="endIndex">The (exclusive) end index to stop reading from the boolean array.</param>
    /// <param name="acked">The boolean array as a reference to write values to from the flag.</param>
    private static void ReadAckFlag(byte flag, int startIndex, int endIndex, ref bool[] acked) {
        byte currentValue = 1;

        for (var i = startIndex; i < endIndex; i++) {
            if ((flag & currentValue) != 0) {
                acked[i] = true;
            }

            currentValue *= 2;
        }
    }
}
