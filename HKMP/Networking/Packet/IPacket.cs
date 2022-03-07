using Hkmp.Math;
using JetBrains.Annotations;

namespace Hkmp.Networking.Packet {
    [PublicAPI]
    public interface IPacket {

        #region Writing integral numeric types

        /**
         * Write one (unsigned) byte to the packet.
         */
        void Write(byte value);

        /**
         * Write an unsigned short (2 bytes) to the packet.
         */
        void Write(ushort value);

        /**
         * Write an unsigned integer (4 bytes) to the packet.
         */
        void Write(uint value);

        /**
         * Write an unsigned long (8 bytes) to the packet.
         */
        void Write(ulong value);
        
        /**
         * Write a signed byte to the packet.
         */
        void Write(sbyte value);

        /**
         * Write a signed short (2 bytes) to the packet.
         */
        void Write(short value);

        /**
         * Write a signed integer (4 bytes) to the packet.
         */
        void Write(int value);
        
        /**
         * Write a signed long (8 bytes) to the packet.
         */
        void Write(long value);
        
        #endregion
        
        #region Writing floating-point numeric types

        /**
         * Write a float (4 bytes) to the packet.
         */
        void Write(float value);

        /**
         * Write a double (8 bytes) to the packet.
         */
        void Write(double value);
        
        #endregion
        
        #region Writing other types

        /**
         * Write a boolean (1 byte) to the packet.
         */
        void Write(bool value);

        /**
         * Write a string value to the packet.
         * The maximum length of a string that can be written is 65,535 (the max value of a ushort).
         * Will throw an exception when used with larger strings.
         */
        void Write(string value);

        /**
         * Write a Vector2 (8 bytes) to the packet.
         * Simply a wrapper for writing the X and Y floats to the packet.
         */
        void Write(Vector2 value);

        #endregion
        
        #region Reading integral numeric types
        
        /**
         * Read one (unsigned) byte from the packet.
         */
        byte ReadByte();

        /**
         * Read an unsigned short (2 bytes) from the packet.
         */
        ushort ReadUShort();

        /**
         * Read an unsigned integer (4 bytes) from the packet.
         */
        uint ReadUInt();

        /**
         * Read an unsigned long (8 bytes) from the packet.
         */
        ulong ReadULong();
        
        /**
         * Read a signed byte from the packet.
         */
        sbyte ReadSByte();

        /**
         * Read a signed short (2 bytes) from the packet.
         */
        short ReadShort();

        /**
         * Read a signed integer (4 bytes) from the packet.
         */
        int ReadInt();

        /**
         * Read a signed long (8 bytes) from the packet.
         */
        long ReadLong();

        #endregion

        #region Reading floating-point numeric types

        /**
         * Read a float (4 bytes) from the packet.
         */
        float ReadFloat();

        /**
         * Read a double (8 bytes) from the packet.
         */
        double ReadDouble();

        #endregion
        
        #region Reading other types

        /**
         * Read a boolean (1 byte) from the packet.
         */
        bool ReadBool();

        /**
         * Read a string value from the packet.
         */
        string ReadString();

        /**
         * Read a Vector2 (8 bytes) from the packet.
         * Simply a wrapper for reading the X and Y floats from the packet.
         */
        Vector2 ReadVector2();

        #endregion
    }
}