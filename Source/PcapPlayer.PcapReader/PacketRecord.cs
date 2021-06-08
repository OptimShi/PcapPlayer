using System.Collections.Generic;

namespace ACE.PcapReader
{
    public class PacketRecord
    {
        public int index;
        public bool isSend;

        // !isPcapng
        public uint tsSec;
        public uint tsUsec;

        // isPcapng
        public uint tsHigh;
        public uint tsLow;


        public string packetHeadersStr;
        public string packetTypeStr;
        public int optionalHeadersLen;
        public List<PacketOpcode> opcodes = new List<PacketOpcode>();
        public string extraInfo;
        public ushort queueID;

        public byte[] data;
        public List<BlobFrag> frags = new List<BlobFrag>();
    }
}
