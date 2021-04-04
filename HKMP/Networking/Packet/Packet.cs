using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HKMP.Networking.Packet {
    public class Packet {
        private readonly List<byte> _buffer;
        private byte[] _readableBuffer;
        private int _readPos;

        /// <summary>Creates a packet from which data can be read. Used for receiving.</summary>
        /// <param name="data">The bytes to add to the packet.</param>
        public Packet(byte[] data) {
            _buffer = new List<byte>(); // Intitialize buffer
            _readPos = 0; // Set readPos to 0

            if (data != null) {
                SetBytes(data);
            }
        }

        // Simply creates an empty packet
        public Packet() {
            _buffer = new List<byte>();
            _readPos = 0;
        }

        // Copies the unread bytes of the given packet into the new one
        protected Packet(Packet packet) : this(packet.ReadBytes(packet.UnreadLength())) {
        }

        #region Functions

        /// <summary>Sets the packet's content and prepares it to be read.</summary>
        /// <param name="data">The bytes to add to the packet.</param>
        private void SetBytes(byte[] data) {
            Write(data);
            _readableBuffer = _buffer.ToArray();
        }

        /// <summary>Inserts the length of the packet's content at the start of the buffer.</summary>
        public void WriteLength() {
            _buffer.InsertRange(0,
                BitConverter.GetBytes((ushort) _buffer.Count)); // Insert the byte length of the packet at the very beginning
        }

        public void InsertSequenceNumber(ushort seqNumber) {
            _buffer.InsertRange(0, BitConverter.GetBytes(seqNumber));
        }

        /// <summary>Inserts the given byte at the start of the buffer.</summary>
        /// <param name="value">The byte to insert.</param>
        protected void InsertByte(byte value) {
            _buffer.InsertRange(0, BitConverter.GetBytes(value)); // Insert the byte at the start of the buffer
        }

        /// <summary>Inserts the given int at the start of the buffer.</summary>
        /// <param name="value">The int to insert.</param>
        protected void InsertInt(int value) {
            _buffer.InsertRange(0, BitConverter.GetBytes(value)); // Insert the int at the start of the buffer
        }

        /// <summary>Gets the packet's content in array form.</summary>
        public byte[] ToArray() {
            return _buffer.ToArray();
        }

        /// <summary>Gets the length of the packet's content.</summary>
        public int Length() {
            return _buffer.Count; // Return the length of buffer
        }

        /// <summary>Gets the length of the unread data contained in the packet.</summary>
        protected int UnreadLength() {
            return Length() - _readPos; // Return the remaining length (unread)
        }

        /// <summary>Resets the packet instance to allow it to be reused.</summary>
        protected void Reset() {
            _buffer.Clear(); // Clear buffer
            _readableBuffer = null;
            _readPos = 0; // Reset readPos
        }

        #endregion

        #region Write Data

        /// <summary>Adds a byte to the packet.</summary>
        /// <param name="value">The byte to add.</param>
        public void Write(byte value) {
            _buffer.Add(value);
        }

        /// <summary>Adds an array of bytes to the packet.</summary>
        /// <param name="value">The byte array to add.</param>
        public void Write(byte[] value) {
            _buffer.AddRange(value);
        }

        /// <summary>Adds a short to the packet.</summary>
        /// <param name="value">The short to add.</param>
        public void Write(short value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds an int to the packet.</summary>
        /// <param name="value">The int to add.</param>
        public void Write(int value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds an ushort to the packet.</summary>
        /// <param name="value">The ushort to add.</param>
        public void Write(ushort value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        public void Write(uint value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a long to the packet.</summary>
        /// <param name="value">The long to add.</param>
        public void Write(long value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a float to the packet.</summary>
        /// <param name="value">The float to add.</param>
        public void Write(float value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a bool to the packet.</summary>
        /// <param name="value">The bool to add.</param>
        public void Write(bool value) {
            _buffer.AddRange(BitConverter.GetBytes(value));
        }

        /// <summary>Adds a string to the packet.</summary>
        /// <param name="value">The string to add.</param>
        public void Write(string value) {
            Write(value.Length); // Add the length of the string to the packet
            _buffer.AddRange(Encoding.ASCII.GetBytes(value)); // Add the string itself
        }

        /// <summary>Adds a Vector3 to the packet.</summary>
        /// <param name="value">The Vector3 to add.</param>
        public void Write(Vector3 value) {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }
        
        /// <summary>Adds a Vector2 to the packet.</summary>
        /// <param name="value">The Vector2 to add.</param>
        public void Write(Vector2 value) {
            Write(value.x);
            Write(value.y);
        }

        /// <summary>Adds a Quaternion to the packet.</summary>
        /// <param name="value">The Quaternion to add.</param>
        public void Write(Quaternion value) {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }

        #endregion

        #region Read Data

        /// <summary>Reads a byte from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte ReadByte(bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // If there are unread bytes
                byte value = _readableBuffer[_readPos]; // Get the byte at readPos' position
                if (moveReadPos) {
                    // If moveReadPos is true
                    _readPos += 1; // Increase readPos by 1
                }

                return value; // Return the byte
            } else {
                throw new Exception("Could not read value of type 'byte'!");
            }
        }

        /// <summary>Reads an array of bytes from the packet.</summary>
        /// <param name="length">The length of the byte array.</param>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte[] ReadBytes(int length, bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // If there are unread bytes
                byte[] value = _buffer.GetRange(_readPos, length)
                    .ToArray(); // Get the bytes at readPos' position with a range of length
                if (moveReadPos) {
                    // If moveReadPos is true
                    _readPos += length; // Increase readPos by length
                }

                return value; // Return the bytes
            }

            return null;
        }

        /// <summary>Reads a short from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public short ReadShort(bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // If there are unread bytes
                var value = BitConverter.ToInt16(_readableBuffer, _readPos); // Convert the bytes to a short
                if (moveReadPos) {
                    // If moveReadPos is true and there are unread bytes
                    _readPos += 2; // Increase readPos by 2
                }

                return value; // Return the short
            }
            
            throw new Exception("Could not read value of type 'short'!");
        }

        public ushort ReadUShort(bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // There are unread bytes
                var value = BitConverter.ToUInt16(_readableBuffer, _readPos);
                if (moveReadPos) {
                    _readPos += 2;
                }

                return value;
            }

            throw new Exception("Could not read value of type 'ushort'!");
        }

        /// <summary>Reads an int from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public int ReadInt(bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // If there are unread bytes
                int value = BitConverter.ToInt32(_readableBuffer, _readPos); // Convert the bytes to an int
                if (moveReadPos) {
                    // If moveReadPos is true
                    _readPos += 4; // Increase readPos by 4
                }

                return value; // Return the int
            } else {
                //throw new Exception("Could not read value of type 'int'!");
            }

            return 1;
        }
        
        /// <summary>Reads an unsigned int from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public uint ReadUInt(bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // If there are unread bytes
                var value = BitConverter.ToUInt32(_readableBuffer, _readPos); // Convert the bytes to an int
                if (moveReadPos) {
                    // If moveReadPos is true
                    _readPos += 4; // Increase readPos by 4
                }

                return value; // Return the int
            } else {
                //throw new Exception("Could not read value of type 'int'!");
            }

            return 1;
        }

        /// <summary>Reads a long from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public long ReadLong(bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // If there are unread bytes
                long value = BitConverter.ToInt64(_readableBuffer, _readPos); // Convert the bytes to a long
                if (moveReadPos) {
                    // If moveReadPos is true
                    _readPos += 8; // Increase readPos by 8
                }

                return value; // Return the long
            } else {
                throw new Exception("Could not read value of type 'long'!");
            }
        }

        /// <summary>Reads a float from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public float ReadFloat(bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // If there are unread bytes
                float value = BitConverter.ToSingle(_readableBuffer, _readPos); // Convert the bytes to a float
                if (moveReadPos) {
                    // If moveReadPos is true
                    _readPos += 4; // Increase readPos by 4
                }

                return value; // Return the float
            } else {
                //throw new Exception("Could not read value of type 'float'!");
            }

            return 1;
        }

        /// <summary>Reads a bool from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public bool ReadBool(bool moveReadPos = true) {
            if (_buffer.Count > _readPos) {
                // If there are unread bytes
                bool value = BitConverter.ToBoolean(_readableBuffer, _readPos); // Convert the bytes to a bool
                if (moveReadPos) {
                    // If moveReadPos is true
                    _readPos += 1; // Increase readPos by 1
                }

                return value; // Return the bool
            } else {
                //throw new Exception("Could not read value of type 'bool'!");
            }

            return false;
        }

        /// <summary>Reads a string from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public string ReadString(bool moveReadPos = true) {
            try {
                int length = ReadInt(); // Get the length of the string
                string value =
                    Encoding.ASCII.GetString(_readableBuffer, _readPos, length); // Convert the bytes to a string
                if (moveReadPos && value.Length > 0) {
                    // If moveReadPos is true string is not empty
                    _readPos += length; // Increase readPos by the length of the string
                }

                return value; // Return the string
            } catch {
                throw new Exception("Could not read value of type 'string'!");
            }
        }

        /// <summary>Reads a Vector3 from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector3 ReadVector3(bool moveReadPos = true) {
            return new Vector3(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }
        
        /// <summary>Reads a Vector2 from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector2 ReadVector2(bool moveReadPos = true) {
            return new Vector2(ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        /// <summary>Reads a Quaternion from the packet.</summary>
        /// <param name="moveReadPos">Whether or not to move the buffer's read position.</param>
        public Quaternion ReadQuaternion(bool moveReadPos = true) {
            return new Quaternion(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos),
                ReadFloat(moveReadPos));
        }

        #endregion
    }
}