using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class Util
{
    public static IDictionary<Type, Func<BinaryReader, dynamic>> readers = null;

    public static ushort byteSwapped(ushort value)
    {
        return (ushort)(((value & 0x00FFU) << 8) | ((value & 0xFF00U) >> 8));
    }

    /// <summary>
    /// Calculates and reads any padding bytes needed to align to a dword boundary.
    /// </summary>
    /// <param name="binaryReader"></param>
    /// <returns>Returns a byte with the number of padding bytes read.</returns>
    public static byte readToAlign(BinaryReader binaryReader)
    {
        long alignDelta = binaryReader.BaseStream.Position % 4;
        if (alignDelta != 0)
        {
            binaryReader.ReadBytes((int)(4 - alignDelta));
            return (byte)(4 - alignDelta);
        }
        return 0;
    }

    public static string readUnicodeString(BinaryReader binaryReader)
    {
        uint strLen = binaryReader.ReadByte();
        // If string length is >= 128 a second byte is present and 
        // the least significant bits are used to calculate the length.
        if ((strLen & 0x80) > 0) // PackedByte
        {
            byte lowbyte = binaryReader.ReadByte();
            strLen = ((strLen & 0x7F) << 8) | lowbyte;
        }
        string str = "";
        if (strLen != 0)
        {
            for (uint i = 0; i < strLen; i++)
            {
                str += Encoding.Unicode.GetString(binaryReader.ReadBytes(2));
            }
        }
        // Note: I had to comment this out to avoid alignment issues. (Slushnas)
        //readToAlign(binaryReader);
        return str;
    }

    public static uint readDataIDOfKnownType(uint i_didFirstID, BinaryReader binaryReader)
    {
        ushort offset = binaryReader.ReadUInt16();

        if ((offset & 0x8000) == 0)
        {
            return i_didFirstID + offset;
        }
        else
        {
            ushort offsetHigh = binaryReader.ReadUInt16();
            return i_didFirstID + (uint)(offsetHigh | ((offset & 0x3FFF) << 16));
        }
    }

    public static uint readWClassIDCompressed(BinaryReader binaryReader)
    {
        ushort id = binaryReader.ReadUInt16();

        if ((id & 0x8000) == 0)
        {
            return id;
        }
        else
        {
            ushort idHigh = binaryReader.ReadUInt16();
            return (uint)(idHigh | ((id & 0x7FFF) << 16));
        }
    }

    public static PacketOpcode readOpcode(BinaryReader fragDataReader)
    {
        PacketOpcode opcode = 0;
        opcode = (PacketOpcode)fragDataReader.ReadUInt32();
        if (opcode == PacketOpcode.WEENIE_ORDERED_EVENT)
        {
            WOrderHdr orderHeader = WOrderHdr.read(fragDataReader);
            opcode = (PacketOpcode)fragDataReader.ReadUInt32();
        }
        if (opcode == PacketOpcode.ORDERED_EVENT)
        {
            OrderHdr orderHeader = OrderHdr.read(fragDataReader);
            opcode = (PacketOpcode)fragDataReader.ReadUInt32();
        }

        return opcode;
    }
}

public class NetBlobIDUtils
{
    public static bool IsEphemeralFlagSet(ulong _id)
    {
        return (_id & 0x8000000000000000) != 0;
    }

    public static ulong GetOrderingType(ulong _id)
    {
        return (_id & 0x1F00000000000000);
    }

    public static ulong GetSequenceID(ulong _id)
    {
        return (_id & 0x00FF0000FFFFFFFF);
    }
}

public class NetBlob
{
    public enum State
    {
        NETBLOB_FROZEN,
        NETBLOB_SENDING,
        NETBLOB_RECEIVING,
        NETBLOB_RECEIVED,
        NETBLOB_FRAGMENTED
    }

    public State state_;
    public byte[] buf_;
    public uint cMaxFragments_;
    public uint numFragments_;
    public ushort sender_;
    public ushort queueID_;
    public uint priority_;
    public NetBlob waitNext_;
    // ulong savedNetBlobID_;
}

public class BlobFrag
{
    //public BlobFragHeader_t hdrWrite_;
    //public BlobFragHeader_t hdrRead_;
    public BlobFragHeader_t memberHeader_;
    public byte[] dat_;
    //public NetBlob myBlob_;
}

public class NetPacket
{
    public enum Flags__guessedname
    {
        npfChecksumEncrypted = (1 << 0),
        npfHasTimeSensitiveHeaders = (1 << 1),
        npfHasSequencedData = (1 << 2),
        npfHasHighPriorityHeaders = (1 << 3)
    }

    public List<COptionalHeader> specialFragList_ = new List<COptionalHeader>();
    public List<BlobFrag> fragList_ = new List<BlobFrag>();
    public ushort recipient_;
    public uint realPriority_;
    public uint size_;
    public uint seqNum_;
    public uint cryptoKey_;
    public uint checksum_;
    public uint flags_;
}

public abstract class Message
{
}

public class EmptyMessage : Message
{
    public PacketOpcode opcode;

    public EmptyMessage(PacketOpcode opcode)
    {
        this.opcode = opcode;
    }
}

public class MessageProcessor
{
    public virtual bool acceptMessageData(BinaryReader messageDataReader)
    {
        return false;
    }
}

public class PStringChar
{
    public string m_buffer;
    public int Length;

    public static PStringChar read(BinaryReader binaryReader)
    {
        PStringChar newObj = new PStringChar();
        var startPosition = binaryReader.BaseStream.Position;
        uint size = binaryReader.ReadUInt16();
        if (size == ushort.MaxValue)
        {
            binaryReader.BaseStream.Seek(-2, SeekOrigin.Current);
            size = binaryReader.ReadUInt32();
        }

        if (size == 0)
        {
            newObj.m_buffer = null;
        }
        else
        {
            newObj.m_buffer = new string(binaryReader.ReadChars((int)size));
        }

        Util.readToAlign(binaryReader);
        newObj.Length = (int)(binaryReader.BaseStream.Position - startPosition);
        return newObj;
    }

    public override string ToString()
    {
        return m_buffer;
    }
}
