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
        var saveDataKeyCount = SaveData.Keys.Count;
        if (saveDataKeyCount > ushort.MaxValue) {
            throw new Exception("Number of keys in save data is too large");
        }

        var dataLength = (ushort) saveDataKeyCount;

        packet.Write(dataLength);

        foreach (var keyValuePair in SaveData) {
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

    /// <inheritdoc />
    public void ReadData(IPacket packet) {
        var dataLength = packet.ReadUShort();

        for (var i = 0; i < dataLength; i++) {
            var saveDataIndex = packet.ReadUShort();

            var length = packet.ReadUShort();
            var value = new byte[length];
            for (var j = 0; j < length; j++) {
                value[j] = packet.ReadByte();
            }

            SaveData.Add(saveDataIndex, value);
        }
    }
}
