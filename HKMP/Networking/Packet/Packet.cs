using System;
using System.Collections.Generic;
using System.Text;
using Hkmp.Logging;
using Hkmp.Math;
using Hkmp.Util;

namespace Hkmp.Networking.Packet;

/// <inheritdoc />
internal class Packet : IPacket {
    /// <summary>
    /// A list of bytes that are contained in this packet.
    /// </summary>
    private readonly List<byte> _buffer;

    /// <summary>
    /// Byte array used as a readable buffer.
    /// </summary>
    private byte[] _readableBuffer;

    /// <summary>
    /// The current position in the buffer to read.
    /// </summary>
    private int _readPos;

    /// <summary>
    /// The length of the packet content.
    /// </summary>
    public int Length => _buffer.Count;

    /// <summary>
    /// Creates a packet with the given byte array of data. Used when receiving packets to read data from.
    /// </summary>
    /// <param name="data"></param>
    public Packet(byte[] data) {
        _buffer = new List<byte>();

        SetBytes(data);
    }

    /// <summary>
    /// Simply creates an empty packet to write data into.
    /// </summary>
    public Packet() {
        _buffer = new List<byte>();
    }

    /// <summary>
    /// Sets the content of the packet to the given byte array of data.
    /// </summary>
    /// <param name="data">The byte to set this packet to.</param>
    private void SetBytes(byte[] data) {
        _buffer.AddRange(data);
        _readableBuffer = _buffer.ToArray();
    }

    /// <summary>
    /// Inserts the length of the packet's content at the start of the buffer.
    /// </summary>
    public void WriteLength() {
        _buffer.InsertRange(
            0,
            BitConverter.GetBytes((ushort) _buffer.Count)
        );
    }

    /// <summary>
    /// Gets the packet's content in array form.
    /// </summary>
    /// <returns>A byte array representing the packet content.</returns>
    public byte[] ToArray() {
        return _buffer.ToArray();
    }

    /// <summary>
    /// Write an array of bytes to the packet.
    /// </summary>
    /// <param name="values">A byte array of values to write.</param>
    public void Write(byte[] values) {
        _buffer.AddRange(values);
    }

    /// <summary>
    /// Read an array of bytes of the given length from the packet.
    /// </summary>
    /// <param name="length">The length to read.</param>
    /// <returns>A byte array of the given length containing the content at the current position in the
    /// packet.</returns>
    /// <exception cref="Exception">Thrown if there are not enough bytes of content left to read.</exception>
    public byte[] ReadBytes(int length) {
        // Check whether there is enough bytes left to read
        if (_buffer.Count >= _readPos + length) {
            var bytes = new byte[length];
            for (var i = 0; i < length; i++) {
                bytes[i] = _readableBuffer[_readPos + i];
            }

            // Increase the reading position in the buffer
            _readPos += length;

            return bytes;
        }

        throw new Exception($"Could not read {length} bytes");
    }

    #region IPacket interface implementations

    #region Writing integral numeric types

    /// <inheritdoc />
    public void Write(byte value) {
        _buffer.Add(value);
    }

    /// <inheritdoc />
    public void Write(ushort value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    /// <inheritdoc />
    public void Write(uint value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    /// <inheritdoc />
    public void Write(ulong value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    /// <inheritdoc />
    public void Write(sbyte value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    /// <inheritdoc />
    public void Write(short value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    /// <inheritdoc />
    public void Write(int value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    /// <inheritdoc />
    public void Write(long value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    #endregion

    #region Writing floating-point numeric types

    /// <inheritdoc />
    public void Write(float value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    /// <inheritdoc />
    public void Write(double value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    #endregion

    #region Writing other types

    /// <inheritdoc />
    public void Write(bool value) {
        _buffer.AddRange(BitConverter.GetBytes(value));
    }

    /// <inheritdoc />
    public void Write(string value) {
        // Encode the string into a byte array with UTF-8
        var byteEncodedString = Encoding.UTF8.GetBytes(value);

        // Check whether we can actually write the length of this string in a unsigned short
        if (byteEncodedString.Length > ushort.MaxValue) {
            throw new Exception($"Could not write string of length: {byteEncodedString.Length} to packet");
        }

        // Write the length of the encoded string and then the byte array itself
        Write((ushort) byteEncodedString.Length);
        Write(byteEncodedString);
    }

    /// <inheritdoc />
    public void Write(Vector2 value) {
        Write(value.X);
        Write(value.Y);
    }

    #endregion

    #region Reading integral numeric types

    /// <inheritdoc />
    public byte ReadByte() {
        // Check whether there is at least 1 byte left to read
        if (_buffer.Count > _readPos) {
            var value = _readableBuffer[_readPos];

            // Increase reading position in the buffer
            _readPos += 1;

            return value;
        }

        throw new Exception("Could not read value of type 'byte'!");
    }

    /// <inheritdoc />
    public ushort ReadUShort() {
        // Check whether there are at least 2 bytes left to read
        if (_buffer.Count > _readPos + 1) {
            var value = BitConverter.ToUInt16(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 2;

            return value;
        }

        throw new Exception("Could not read value of type 'ushort'!");
    }

    /// <inheritdoc />
    public uint ReadUInt() {
        // Check whether there are at least 4 bytes left to read
        if (_buffer.Count > _readPos + 3) {
            var value = BitConverter.ToUInt32(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 4;

            return value;
        }

        throw new Exception("Could not read value of type 'uint'!");
    }

    /// <inheritdoc />
    public ulong ReadULong() {
        // Check whether there are at least 8 bytes left to read
        if (_buffer.Count > _readPos + 7) {
            var value = BitConverter.ToUInt64(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 8;

            return value;
        }

        throw new Exception("Could not read value of type 'ulong'!");
    }

    /// <inheritdoc />
    public sbyte ReadSByte() {
        // Check whether there are at least 1 byte left to read
        if (_buffer.Count > _readPos) {
            var value = (sbyte) _readableBuffer[_readPos];

            // Increase the reading position in the buffer
            _readPos += 1;

            return value;
        }

        throw new Exception("Could not read value of type 'sbyte'!");
    }

    /// <inheritdoc />
    public short ReadShort() {
        // Check whether there are at least 2 bytes left to read
        if (_buffer.Count > _readPos + 1) {
            var value = BitConverter.ToInt16(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 2;

            return value;
        }

        throw new Exception("Could not read value of type 'short'!");
    }

    /// <inheritdoc />
    public int ReadInt() {
        // Check whether there are at least 4 bytes left to read
        if (_buffer.Count > _readPos + 3) {
            var value = BitConverter.ToInt32(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 4;

            return value;
        }

        throw new Exception("Could not read value of type 'int'!");
    }

    /// <inheritdoc />
    public long ReadLong() {
        // Check whether there are at least 8 bytes left to read
        if (_buffer.Count > _readPos + 7) {
            var value = BitConverter.ToInt64(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 8;

            return value;
        }

        throw new Exception("Could not read value of type 'long'!");
    }

    #endregion

    #region Reading floating-point numeric types

    /// <inheritdoc />
    public float ReadFloat() {
        // Check whether there are at least 4 bytes left to read
        if (_buffer.Count > _readPos + 3) {
            var value = BitConverter.ToSingle(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 4;

            return value;
        }

        throw new Exception("Could not read value of type 'float'!");
    }

    /// <inheritdoc />
    public double ReadDouble() {
        // Check whether there are at least 8 bytes left to read
        if (_buffer.Count > _readPos + 7) {
            var value = BitConverter.ToDouble(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 8;

            return value;
        }

        throw new Exception("Could not read value of type 'double'!");
    }

    #endregion

    #region Reading other types

    /// <inheritdoc />
    public bool ReadBool() {
        // Check whether there is at least 1 byte left to read
        if (_buffer.Count > _readPos) {
            var value = BitConverter.ToBoolean(_readableBuffer, _readPos);

            // Increase the reading position in the buffer
            _readPos += 1;

            return value;
        }

        throw new Exception("Could not read value of type 'bool'!");
    }

    /// <inheritdoc />
    public string ReadString() {
        // First read the length of the string as an unsigned short, which implicitly checks
        // whether there are at least 2 bytes left to read
        var length = ReadUShort();

        // Edge case if the length is zero, we simply return an empty string already
        if (length == 0) {
            return "";
        }

        // Now we check whether there are at least as many bytes left to read as the length of the string
        if (_buffer.Count < _readPos + length) {
            throw new Exception("Could not read value of type 'string'!");
        }

        // Now we read and decode the string
        var value = Encoding.UTF8.GetString(_readableBuffer, _readPos, length);

        // Increase the reading position in the buffer
        _readPos += length;

        return value;
    }

    /// <inheritdoc />
    public Vector2 ReadVector2() {
        // Simply construct the Vector2 by reading a float from the packet twice, which should
        // check whether there are enough bytes left to read and throw exceptions if not
        return new Vector2(ReadFloat(), ReadFloat());
    }

    #endregion

    #endregion
}
