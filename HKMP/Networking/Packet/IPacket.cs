using System;
using System.Collections.Generic;
using Hkmp.Math;

namespace Hkmp.Networking.Packet;

/// <summary>
/// Packet for reading and writing raw values.
/// </summary>
public interface IPacket {
    #region Writing integral numeric types

    /// <summary>
    /// Write one (unsigned) byte to the packet.
    /// </summary>
    /// <param name="value">The byte value.</param>
    void Write(byte value);

    /// <summary>
    /// Write an unsigned short (2 bytes) to the packet.
    /// </summary>
    /// <param name="value">The unsigned short value.</param>
    void Write(ushort value);

    /// <summary>
    /// Write an unsigned integer (4 bytes) to the packet.
    /// </summary>
    /// <param name="value">The unsigned integer value.</param>
    void Write(uint value);

    /// <summary>
    /// Write an unsigned long (8 bytes) to the packet.
    /// </summary>
    /// <param name="value">The unsigned long value.</param>
    void Write(ulong value);

    /// <summary>
    /// Write a signed byte to the packet.
    /// </summary>
    /// <param name="value">The signed byte value.</param>
    void Write(sbyte value);

    /// <summary>
    /// Write a signed short (2 bytes) to the packet.
    /// </summary>
    /// <param name="value">The signed short value.</param>
    void Write(short value);

    /// <summary>
    /// Write a signed integer (4 bytes) to the packet.
    /// </summary>
    /// <param name="value">The signed integer value.</param>
    void Write(int value);

    /// <summary>
    /// Write a signed long (8 bytes) to the packet.
    /// </summary>
    /// <param name="value">The signed long value.</param>
    void Write(long value);

    #endregion

    #region Writing floating-point numeric types

    /// <summary>
    /// Write a float (4 bytes) to the packet.
    /// </summary>
    /// <param name="value">The floating point value.</param>
    void Write(float value);

    /// <summary>
    /// Write a double (8 bytes) to the packet.
    /// </summary>
    /// <param name="value">The double precision floating point value.</param>
    void Write(double value);

    #endregion

    #region Writing other types

    /// <summary>
    /// Write a boolean (1 byte) to the packet.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    void Write(bool value);

    /// <summary>
    /// Write a string value to the packet. The maximum length of a string that can be written is 65,535
    /// (the max value of a ushort). Will throw an exception when used with larger strings.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="Exception">Thrown when the length of the given string is larger than 65535.</exception>
    void Write(string value);

    /// <summary>
    /// Write a Vector2 (8 bytes) to the packet. Simply a wrapper for writing the X and Y floats to the packet.
    /// </summary>
    /// <param name="value">The Vector2 value.</param>
    void Write(Vector2 value);
    
    /// <summary>
    /// Write a Vector3 (12 bytes) to the packet. Simply a wrapper for writing the X, Y and Z floats to the packet.
    /// </summary>
    /// <param name="value">The Vector3 value.</param>
    void Write(Vector3 value);

    /// <summary>
    /// Write a bit flag to the packet based on an enum type and a set of that type. Will write either a byte,
    /// unsigned short, unsigned int or unsigned long to the packet based on the size of the enum. Also assumes that
    /// the enum has underlying int values starting from 0 and incrementing by 1 for each subsequent type.
    /// </summary>
    /// <param name="set">The set containing the values for which a bit in the flag should be set to 1.</param>
    /// <typeparam name="TEnum">The enum type that the set also uses.</typeparam>
    void WriteBitFlag<TEnum>(ISet<TEnum> set) where TEnum : Enum;

    #endregion

    #region Reading integral numeric types

    /// <summary>
    /// Read one (unsigned) byte from the packet.
    /// </summary>
    /// <returns>The unsigned byte value.</returns>
    byte ReadByte();

    /// <summary>
    /// Read an unsigned short (2 bytes) from the packet.
    /// </summary>
    /// <returns>The unsigned short value.</returns>
    ushort ReadUShort();

    /// <summary>
    /// Read an unsigned integer (4 bytes) from the packet.
    /// </summary>
    /// <returns>The unsigned integer value.</returns>
    uint ReadUInt();

    /// <summary>
    /// Read an unsigned long (8 bytes) from the packet.
    /// </summary>
    /// <returns>The unsigned long value.</returns>
    ulong ReadULong();

    /// <summary>
    /// Read a signed byte from the packet.
    /// </summary>
    /// <returns>The signed byte value.</returns>
    sbyte ReadSByte();

    /// <summary>
    /// Read a signed short (2 bytes) from the packet.
    /// </summary>
    /// <returns>The signed short value.</returns>
    short ReadShort();

    /// <summary>
    /// Read a signed integer (4 bytes) from the packet.
    /// </summary>
    /// <returns>The signed integer value.</returns>
    int ReadInt();

    /// <summary>
    /// Read a signed long (8 bytes) from the packet.
    /// </summary>
    /// <returns>The signed long value.</returns>
    long ReadLong();

    #endregion

    #region Reading floating-point numeric types

    /// <summary>
    /// Read a float (4 bytes) from the packet.
    /// </summary>
    /// <returns>The floating point value.</returns>
    float ReadFloat();

    /// <summary>
    /// Read a double (8 bytes) from the packet.
    /// </summary>
    /// <returns>The double precision floating point value.</returns>
    double ReadDouble();

    #endregion

    #region Reading other types

    /// <summary>
    /// Read a boolean (1 byte) from the packet.
    /// </summary>
    /// <returns>The boolean value.</returns>
    bool ReadBool();

    /// <summary>
    /// Read a string value from the packet.
    /// </summary>
    /// <returns>The string value.</returns>
    string ReadString();

    /// <summary>
    /// Read a Vector2 (8 bytes) from the packet. Simply a wrapper for reading the X and Y floats from the packet.
    /// </summary>
    /// <returns>The Vector2 value.</returns>
    Vector2 ReadVector2();
    
    /// <summary>
    /// Read a Vector3 (12 bytes) from the packet. Simply a wrapper for reading the X, Y and Z floats from the packet.
    /// </summary>
    /// <returns>The Vector3 value.</returns>
    Vector3 ReadVector3();
    
    /// <summary>
    /// Read a bit flag from the packet based on an enum type and a set of that type. Will read either a byte,
    /// unsigned short, unsigned int or unsigned long from the packet based on the size of the enum. Also assumes that
    /// the enum has underlying int values starting from 0 and incrementing by 1 for each subsequent type.
    /// </summary>
    /// <returns>The set containing the enum values where the corresponding bit in the flag was set to 1.</returns>
    /// <typeparam name="TEnum">The enum type that the set also uses.</typeparam>
    ISet<TEnum> ReadBitFlag<TEnum>() where TEnum : Enum;

    #endregion
}
