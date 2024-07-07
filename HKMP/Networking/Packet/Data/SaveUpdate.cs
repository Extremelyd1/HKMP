using System;
using System.Collections.Generic;

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

        if (Value.Length > ushort.MaxValue) {
            throw new Exception($"Number of bytes exceeds ushort max value: {Value.Length}");
        }

        var length = (ushort) Value.Length;
        packet.Write(length);
        for (var i = 0; i < length; i++) {
            packet.Write(Value[i]);
        }
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        SaveDataIndex = packet.ReadUShort();

        var length = packet.ReadUShort();
        Value = new byte[length];
        for (var i = 0; i < length; i++) {
            Value[i] = packet.ReadByte();
        }
    }
}

/// <summary>
/// Packet data for when an entire save is networked.
/// </summary>
internal class CurrentSave : IPacketData {
    /// <inheritdoc />
    public bool IsReliable => true;

    /// <inheritdoc />
    public bool DropReliableDataIfNewerExists => false;
    
    public Dictionary<ushort, byte[]> SaveData { get; set; }

    public CurrentSave() {
        SaveData = new Dictionary<ushort, byte[]>();
    }

    /// <inheritdoc />
    public void WriteData(IPacket packet) {
        WriteSaveDataDict(SaveData, packet);
    }

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        SaveData = ReadSaveDataDict(packet);
    }

    /// <summary>
    /// Writes a save data dictionary to the given packet.
    /// </summary>
    /// <param name="dataDict">The dictionary mapping indices to byte encoded values to write.</param>
    /// <param name="packet">The packet to write the data into.</param>
    /// <exception cref="Exception">Thrown if the number of keys in the given save data dictionary is too large to
    /// be written (> max ushort).</exception>
    public static void WriteSaveDataDict(Dictionary<ushort, byte[]> dataDict, IPacket packet) {
        var saveDataKeyCount = dataDict.Keys.Count;
        if (saveDataKeyCount > ushort.MaxValue) {
            throw new Exception("Number of keys in save data is too large");
        }

        var dataLength = (ushort) saveDataKeyCount;

        packet.Write(dataLength);

        foreach (var keyValuePair in dataDict) {
            var saveDataIndex = keyValuePair.Key;
            var value = keyValuePair.Value;
            
            packet.Write(saveDataIndex);

            var length = (ushort) System.Math.Min(value.Length, ushort.MaxValue);
            packet.Write(length);
            for (var i = 0; i < length; i++) {
                packet.Write(value[i]);
            }
        }
    }

    /// <summary>
    /// Reads a save data dictionary that maps indices to byte encoded values from the given
    /// packet.
    /// </summary>
    /// <param name="packet">The packet interface to read from.</param>
    /// <returns>A dictionary mapping save data indices to byte encoded values.</returns>
    public static Dictionary<ushort, byte[]> ReadSaveDataDict(IPacket packet) {
        var saveData = new Dictionary<ushort, byte[]>();
        var dataLength = packet.ReadUShort();

        for (var i = 0; i < dataLength; i++) {
            var saveDataIndex = packet.ReadUShort();

            var length = packet.ReadUShort();
            var value = new byte[length];
            for (var j = 0; j < length; j++) {
                value[j] = packet.ReadByte();
            }

            saveData.Add(saveDataIndex, value);
        }

        return saveData;
    }
}
