using System;
using System.Collections.Generic;
using System.Text;
using Hkmp.Math;

namespace Hkmp.Networking.Packet {
    public class Packet : IPacket {
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
         * Write an array of bytes to the packet.
         */
        internal void Write(byte[] values) {
            _buffer.AddRange(values);
        }

        /**
         * Read an array of bytes of the given length from the packet.
         */
        internal byte[] ReadBytes(int length) {
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
        
        /**
         * Whether this packet has data left to read.
         */
        public bool HasDataLeft() {
            return _buffer.Count > _readPos;
        }
        
        #region IPacket interface implementations
        
        #region Writing integral numeric types
        
        public void Write(byte value) {
            _buffer.Add(value);
        }
        
        public void Write(ushort value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(uint value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
        
        public void Write(ulong value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
        
        public void Write(sbyte value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
        
        public void Write(short value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
        
        public void Write(int value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
        
        public void Write(long value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
        
        #endregion
        
        #region Writing floating-point numeric types

        public void Write(float value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
        
        public void Write(double value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }
        
        #endregion
        
        #region Writing other types

        public void Write(bool value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

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

        public void Write(Vector2 value) {
            Write(value.X);
            Write(value.Y);
        }
        
        #endregion
        
        #region Reading integral numeric types
        
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

        public Vector2 ReadVector2() {
            // Simply construct the Vector2 by reading a float from the packet twice, which should
            // check whether there are enough bytes left to read and throw exceptions if not
            return new Vector2(ReadFloat(), ReadFloat());
        }

        #endregion
        #endregion
    }
}