using System;
using System.Collections.Generic;
using System.Text;
using Hkmp.Math;

namespace Hkmp.Networking.Packet {
    public class Packet {
        private readonly List<byte> _buffer;
        private byte[] _readableBuffer;
        private int _readPos;

        // The length of the packet content
        public int Length => _buffer.Count;
        
        /**
         * Creates a packet with the given byte array of data. Used when receiving packets to read data from.
         */
        public Packet(byte[] data) {
            _buffer = new List<byte>();

            SetBytes(data);
        }

        /**
         * Simply creates an empty packet to write data into.
         */
        public Packet() {
            _buffer = new List<byte>();
        }

        /**
         * Sets the content of the packet to the given byte array of data.
         */
        private void SetBytes(byte[] data) {
            _buffer.AddRange(data);
            _readableBuffer = _buffer.ToArray();
        }

        /**
         * Inserts the length of the packet's content at the start of the buffer.
         */
        public void WriteLength() {
            _buffer.InsertRange(
                0,
                BitConverter.GetBytes((ushort) _buffer.Count)
            );
        }

        /**
         * Gets the packet's content in array form.
         */
        public byte[] ToArray() {
            return _buffer.ToArray();
        }

        /**
         * Write one byte to the packet.
         */
        public void Write(byte value) {
            _buffer.Add(value);
        }

        /**
         * Write an array of bytes to the packet.
         */
        private void Write(byte[] bytes) {
            _buffer.AddRange(bytes);
        }

        /**
         * Write an unsigned integer (4 bytes) to the packet.
         */
        public void Write(uint value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /**
         * Write an unsigned short (2 bytes) to the packet.
         */
        public void Write(ushort value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /**
         * Write a float to the packet.
         */
        public void Write(float value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /**
         * Write a bool (in one byte) to the packet.
         */
        public void Write(bool value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /**
         * Write a string value to the packet.
         */
        public void Write(string value) {
            // Encode the string into a byte array
            var byteEncodedString = Encoding.ASCII.GetBytes(value);
            
            // Check whether we can actually write the length of this string in a unsigned short
            if (byteEncodedString.Length > ushort.MaxValue) {
                throw new Exception($"Could not write string of length: {byteEncodedString.Length} to packet");
            }
            
            // Write the length of the encoded string and then the string itself
            Write((ushort) byteEncodedString.Length);
            Write(byteEncodedString);
        }

        /**
         * Write a Vector2 to the packet.
         */
        public void Write(Vector2 value) {
            Write(value.X);
            Write(value.Y);
        }

        /**
         * Read a single byte from the packet.
         */
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

        /**
         * Read an unsigned short (2 bytes) from the packet.
         */
        public ushort ReadUShort() {
            // Check whether there are at least two bytes left to read
            if (_buffer.Count > _readPos + 1) {
                var value = BitConverter.ToUInt16(_readableBuffer, _readPos);
                
                // Increase the reading position in the buffer
                _readPos += 2;

                return value;
            }

            throw new Exception("Could not read value of type 'ushort'!");
        }

        /**
         * Read an unsigned integer (4 bytes) from the packet.
         */
        public uint ReadUInt() {
            // Check whether there are at least 4 bytes left to read
            if (_buffer.Count > _readPos + 3) {
                var value = BitConverter.ToUInt32(_readableBuffer, _readPos);
                
                // Increase the reading position in the buffer
                _readPos += 4; 

                return value;
            }

            throw new Exception("Could not read value of type 'int'!");
        }

        /**
         * Read a float (4 bytes) from the packet.
         */
        private float ReadFloat() {
            // Check whether there are at least 4 bytes left to read
            if (_buffer.Count > _readPos + 3) {
                var value = BitConverter.ToSingle(_readableBuffer, _readPos);
                
                // Increase the reading position in the buffer
                _readPos += 4;

                return value;
            }

            throw new Exception("Could not read value of type 'float'!");
        }

        /**
         * Read a bool (1 byte) from the packet.
         */
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

        /**
         * Read a string from the packet.
         */
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

            // Actually read the string
            var value = Encoding.ASCII.GetString(_readableBuffer, _readPos, length);
            
            // Increase the reading position in the buffer
            _readPos += length;

            return value;
        }

        /**
         * Read a Vector2 (8 bytes) from the packet.
         */
        public Vector2 ReadVector2() {
            // Simply construct the Vector2 by reading a float from the packet twice, which should
            // check whether there are enough bytes left to read and throw exceptions if not
            return new Vector2(ReadFloat(), ReadFloat());
        }

        /**
         * Whether this packet has data left to read.
         */
        public bool HasDataLeft() {
            return _buffer.Count > _readPos;
        }
    }
}