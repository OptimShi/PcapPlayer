using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ACE.PcapReader
{
    public static partial class PCapReader
    {
        public static List<PacketRecord> Records;
        public static int StartRecordIndex;
        public static int EndRecordIndex;
        public static int PausedRecordIndex; // Record index of the LoginCompleteNotification from the client

        // Note that throughout the project Index will mean zero-based and Instance will mean one-based
        public static int LoginInstances;
        public static List<int> LoginIndexes; // List of line numbers of pcap login instances
        public static List<PcapMarker> PcapMarkers; // List of login and teleport events
        public static int CurrentLoginInstance;
        // Key is login instance (one-based), Value is a list of teleport line numbers
        public static Dictionary<int, List<int>> TeleportIndexes; 

        public static uint StartTime;
        public static uint CharacterGUID;
        public static bool HasLoginEvent;

        // Player's starting position
        public static uint PlayerObjcell;
        public static float PlayerX;
        public static float PlayerY;
        public static float PlayerZ;
        public static float PlayerQW = 1;
        public static float PlayerQX = 0;
        public static float PlayerQY = 0;
        public static float PlayerQZ = 0;

        public static bool IsPcapPng;

        public static int CurrentPcapRecordStart;

        public static void Initialize()
        {
            Records = new List<PacketRecord>();
            StartRecordIndex = 0;
            EndRecordIndex = 0;
            StartTime = 0;
            PausedRecordIndex = 0;
            CharacterGUID = 0;
            CurrentPcapRecordStart = 0;
            HasLoginEvent = false;
            TeleportIndexes = new Dictionary<int, List<int>>();
            LoginIndexes = new List<int>();
            PcapMarkers = new List<PcapMarker>();
        }

        public static void Reset()
        {
            TeleportIndexes.Clear();
            LoginIndexes.Clear();
            PcapMarkers.Clear();
        }

        // Set the start and end record positions in the pcap
        public static void SetLoginInstance(int instanceID)
        {
            CurrentLoginInstance = 0;
            StartRecordIndex = 0;
            EndRecordIndex = 0;

            if (LoginInstances == 0)
            {
                HasLoginEvent = false;
                EndRecordIndex = Records.Count - 1;

                // Merge with the "Base Login" pcap.
                LoadLoginPcap();
                return;
            }

            HasLoginEvent = true;

            // Set to one if they specify an instance that does not exist...
            if (instanceID > LoginInstances)
                instanceID = 1;

            int loginsFound = 0;
            for (int i = 0; i < Records.Count; i++)
            {
                // Search through the login events to find the start
                if (Records[i].opcodes.Count > 0 && Records[i].opcodes[0] == PacketOpcode.CHARACTER_ENTER_GAME_EVENT)
                {
                    loginsFound++;
                    if (loginsFound == instanceID)
                    {
                        CurrentLoginInstance = instanceID;
                        StartRecordIndex = i;
                        StartTime = Records[i].tsSec;

                        // Get the Character GUID from the login event... This is hacky, but it works!
                        string cGUID = "0x" +
                                       Records[i].data[7].ToString("X2") +
                                       Records[i].data[6].ToString("X2") +
                                       Records[i].data[5].ToString("X2") +
                                       Records[i].data[4].ToString("X2");
                        CharacterGUID = Convert.ToUInt32(cGUID, 16);

                        // If there's only one login, or we're on the last, the log plays to the end...
                        if (LoginInstances == 1 || instanceID == LoginInstances)
                        {
                            EndRecordIndex = Records.Count;
                            return;
                        }
                    }
                }

                if (loginsFound == (instanceID + 1) &&
                    Records[i].opcodes[0] == PacketOpcode.Evt_Character__LoginCompleteNotification_ID)
                {
                    PausedRecordIndex = i;
                }

                // Time to try to find the EndRecordIndex
                if (Records[i].opcodes.Count > 0 && Records[i].opcodes[0] == PacketOpcode.CHARACTER_EXIT_GAME_EVENT &&
                    loginsFound == (instanceID + 1))
                {
                    EndRecordIndex = i;
                    return;
                }
            }

            if (EndRecordIndex == 0)
            {
                EndRecordIndex = Records.Count;
            }
        }

        // Set the number of logins in this pcap
        private static void SetLoginInstanceCount()
        {
            if (Records.Count == 0)
            {
                LoginInstances = 0;
                return;
            }

            LoginInstances = 0;
            int teleports = 0;
            for (int i = 0; i < Records.Count; i++)
            {
                if (Records[i].opcodes.Count > 0 &&
                    Records[i].opcodes[0] == PacketOpcode.Evt_Character__EnterGame_ServerReady_ID)
                {
                    LoginInstances++;
                    LoginIndexes.Add(i);
                    PcapMarkers.Add(new PcapMarker(MarkerType.Login, i, LoginInstances));
                    teleports = 0;
                }
                else if (Records[i].opcodes.Count > 0 &&
                         Records[i].opcodes[0] == PacketOpcode.Evt_Physics__PlayerTeleport_ID)
                {
                    teleports++;
                    if (!TeleportIndexes.ContainsKey(LoginInstances))
                        TeleportIndexes.Add(LoginInstances, new List<int> {i});
                    else
                        TeleportIndexes[LoginInstances].Add(i);
                    PcapMarkers.Add(new PcapMarker(MarkerType.Teleport, i, LoginInstances));
                }
            }
        }

        public static void GetPcapDuration()
        {
            var endRecordTime = GetTimestamp(Records[EndRecordIndex - 1]);
            var startRecordTime = GetTimestamp(Records[StartRecordIndex]);

            TimeSpan duration = endRecordTime - startRecordTime;
            string elapsedTime = String.Format("{0:00} hours, {1:00} minutes, {2:00} seconds",
                duration.Hours, duration.Minutes, duration.Seconds);
            Console.WriteLine("Pcap duration is " + elapsedTime);
        }

        private static DateTime GetTimestamp(PacketRecord record)
        {
            if (IsPcapPng)
            {
                long microseconds = record.tsHigh;
                microseconds = (microseconds << 32) | record.tsLow;
                var ticks = Convert.ToInt64(microseconds * 10);
                var time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(ticks);
                return time;
            }
            else
            {
                var ticks = Convert.ToInt64(record.tsUsec * 10);
                var time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(record.tsSec).AddTicks(ticks);
                return time;
            }
        }

        public static void LoadPcap(string fileName, bool asMessages, ref bool abort)
        {
            using (FileStream fileStream =
                new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    uint magicNumber = binaryReader.ReadUInt32();

                    binaryReader.BaseStream.Position = 0;

                    List<PacketRecord> allRecords;
                    if (magicNumber == 0xA1B2C3D4 || magicNumber == 0xD4C3B2A1)
                    {
                        allRecords = loadPcapPacketRecords(binaryReader, asMessages, ref abort);
                        IsPcapPng = false;
                    }
                    else
                    {
                        allRecords = loadPcapngPacketRecords(binaryReader, asMessages, ref abort);
                        IsPcapPng = true;
                    }

                    /*
                    for(int i = 0; i < allRecords.Count; i++)
                    {
                        // Only load the server-to-client messages, because that is all we actually care about
                        if (allRecords[i].isSend == false)
                            Records.Add(allRecords[i]);
                    }
                    */
                    Records = allRecords;
                }
            }

            Reset();
            SetLoginInstanceCount();

            SetLoginInstance(1);
            GetPcapDuration();
        }

        /// <summary>
        /// Sends the player to the next Teleport instance in the Pcap (if any!)
        /// </summary>
        public static bool DoTeleport(int? teleportID)
        {
            // Try to load the next teleport if no index was passed
            if (teleportID == null)
            {
                try
                {
                    var newIndex = TeleportIndexes[CurrentLoginInstance].First(index => (index >= CurrentPcapRecordStart));
                    CurrentPcapRecordStart = newIndex - 1;
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught Exception: " + e.Message);
                    return false;
                }
            }

            // Decrement because user should supply a one-based index
            teleportID--;
            if (teleportID < 0)
                return false;

            // Go to a specific teleport index
            if (teleportID < TeleportIndexes[CurrentLoginInstance].Count)
            {
                CurrentPcapRecordStart = TeleportIndexes[CurrentLoginInstance][(int) teleportID] - 1;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a string containing detailed location info where a packet is located.
        /// Scans forward to find the next movement event and returns a string containing loc, coordinates (if overground), and dungeon name (if appropriate)
        /// Note that we don't care WHAT is moving, just any item within the player's "vision" is good enough to get an approximate location.
        /// </summary>
        /// <param name="packetID">The packet of our event to start searching from</param>
        /// <param name="endPpacket">The end packet to search until.</param>
        /// <returns></returns>
        public static CM_Movement.Position GetDetailedLocationInfo(int packetID, int endPpacket = -1)
        {
            for(var i = packetID; i < endPpacket; i++)
            {
                switch(Records[i].opcodes[0]){
                    /*
                    case PacketOpcode.Evt_Movement__AutonomousPosition_ID:
                        using (BinaryReader br = new BinaryReader(new MemoryStream(Records[i].data)))
                        {
                            Util.readOpcode(br);
                            var autoMsg = CM_Movement.AutonomousPosition.read(br);
                            return autoMsg.position;
                        }
                        break;
                    */
                    case PacketOpcode.Evt_Movement__MoveToState_ID:
                        using (BinaryReader br = new BinaryReader(new MemoryStream(Records[i].data)))
                        {
                            Util.readOpcode(br);
                            var moveMsg = CM_Movement.MoveToState.read(br);
                            return moveMsg.position;
                            //var acePos = new ACE.Entity.Position();
                        }
                        break;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the timestamp of an event, relative to its login instance
        /// </summary>
        /// <param name="marker">A PcapMarker of the event in question</param>
        /// <param name="format">The TimeSpan format of the response</param>
        /// <returns>A string with a readable time</returns>
        public static string GetPcapTime(PcapMarker marker, string format = "g")
        {
            // Get the time of the relative Login Event
            var loginEvtRecord = LoginIndexes[marker.LoginInstance-1];
            var loginTime = GetTimestamp(Records[loginEvtRecord]);

            // Get the time of the packetID
            var packetTime = GetTimestamp(Records[marker.LineNumber]);

            // Do the math.
            var diff = packetTime - loginTime;

            // Profit
            return diff.ToString(@"hh\:mm\:ss");
            return diff.ToString(format);
        }

        private class FragNumComparer : IComparer<BlobFrag>
        {
            int IComparer<BlobFrag>.Compare(BlobFrag a, BlobFrag b)
            {
                if (a.memberHeader_.blobNum > b.memberHeader_.blobNum)
                    return 1;
                if (a.memberHeader_.blobNum < b.memberHeader_.blobNum)
                    return -1;
                else
                    return 0;
            }
        }

        private static bool addPacketIfFinished(List<PacketRecord> finishedRecords, PacketRecord record)
        {
            record.frags.Sort(new FragNumComparer());

            // Make sure all fragments are present
            if (record.frags.Count < record.frags[0].memberHeader_.numFrags
                || record.frags[0].memberHeader_.blobNum != 0
                || record.frags[record.frags.Count - 1].memberHeader_.blobNum !=
                record.frags[0].memberHeader_.numFrags - 1)
            {
                return false;
            }

            record.index = finishedRecords.Count;

            // Remove duplicate fragments
            int index = 0;
            while (index < record.frags.Count - 1)
            {
                if (record.frags[index].memberHeader_.blobNum == record.frags[index + 1].memberHeader_.blobNum)
                    record.frags.RemoveAt(index);
                else
                    index++;
            }

            int totalMessageSize = 0;
            foreach (BlobFrag frag in record.frags)
            {
                totalMessageSize += frag.dat_.Length;
            }

            record.data = new byte[totalMessageSize];
            int offset = 0;
            foreach (BlobFrag frag in record.frags)
            {
                Buffer.BlockCopy(frag.dat_, 0, record.data, offset, frag.dat_.Length);
                offset += frag.dat_.Length;
            }

            finishedRecords.Add(record);

            return true;
        }

        private static PcapRecordHeader readPcapRecordHeader(BinaryReader binaryReader, int curPacket)
        {
            if (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position < 16)
            {
                throw new InvalidDataException("Stream cut short (packet " + curPacket + "), stopping read: " +
                                               (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position));
            }

            PcapRecordHeader recordHeader = PcapRecordHeader.read(binaryReader);

            if (recordHeader.inclLen > 50000)
            {
                throw new InvalidDataException("Enormous packet (packet " + curPacket + "), stopping read: " +
                                               recordHeader.inclLen);
            }

            // Make sure there's enough room for an ethernet header
            if (recordHeader.inclLen < 14)
            {
                binaryReader.BaseStream.Position += recordHeader.inclLen;
                return null;
            }

            return recordHeader;
        }

        private static List<PacketRecord> loadPcapPacketRecords(BinaryReader binaryReader, bool asMessages,
            ref bool abort)
        {
            List<PacketRecord> results = new List<PacketRecord>();

            /*PcapHeader pcapHeader = */
            PcapHeader.read(binaryReader);

            int curPacket = 0;

            Dictionary<ulong, PacketRecord> incompletePacketMap = new Dictionary<ulong, PacketRecord>();

            while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
            {
                if (abort)
                    break;

                PcapRecordHeader recordHeader;
                try
                {
                    recordHeader = readPcapRecordHeader(binaryReader, curPacket);

                    if (recordHeader == null)
                    {
                        continue;
                    }
                }
                catch (InvalidDataException)
                {
                    break;
                }

                long packetStartPos = binaryReader.BaseStream.Position;

                try
                {
                    if (asMessages)
                    {
                        if (!readMessageData(binaryReader, recordHeader.inclLen, recordHeader.tsSec,
                            recordHeader.tsUsec, results, incompletePacketMap))
                            break;
                    }
                    else
                    {
                        var packetRecord = readPacketData(binaryReader, recordHeader.inclLen, recordHeader.tsSec,
                            recordHeader.tsUsec, curPacket);

                        if (packetRecord == null)
                            break;

                        results.Add(packetRecord);
                    }

                    curPacket++;
                }
                catch (Exception)
                {
                    binaryReader.BaseStream.Position +=
                        recordHeader.inclLen - (binaryReader.BaseStream.Position - packetStartPos);
                }
            }

            return results;
        }

        private static PcapngBlockHeader readPcapngBlockHeader(BinaryReader binaryReader, int curPacket)
        {
            if (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position < 8)
            {
                throw new InvalidDataException("Stream cut short (packet " + curPacket + "), stopping read: " +
                                               (binaryReader.BaseStream.Length - binaryReader.BaseStream.Position));
            }

            long blockStartPos = binaryReader.BaseStream.Position;

            PcapngBlockHeader blockHeader = PcapngBlockHeader.read(binaryReader);

            if (blockHeader.capturedLen > 50000)
            {
                throw new InvalidDataException("Enormous packet (packet " + curPacket + "), stopping read: " +
                                               blockHeader.capturedLen);
            }

            // Make sure there's enough room for an ethernet header
            if (blockHeader.capturedLen < 14)
            {
                binaryReader.BaseStream.Position +=
                    blockHeader.blockTotalLength - (binaryReader.BaseStream.Position - blockStartPos);
                return null;
            }

            return blockHeader;
        }

        private static List<PacketRecord> loadPcapngPacketRecords(BinaryReader binaryReader, bool asMessages,
            ref bool abort)
        {
            List<PacketRecord> results = new List<PacketRecord>();

            int curPacket = 0;

            Dictionary<ulong, PacketRecord> incompletePacketMap = new Dictionary<ulong, PacketRecord>();

            while (binaryReader.BaseStream.Position != binaryReader.BaseStream.Length)
            {
                if (abort)
                    break;

                long blockStartPos = binaryReader.BaseStream.Position;

                PcapngBlockHeader blockHeader;
                try
                {
                    blockHeader = readPcapngBlockHeader(binaryReader, curPacket);

                    if (blockHeader == null)
                    {
                        continue;
                    }
                }
                catch (InvalidDataException)
                {
                    break;
                }

                long packetStartPos = binaryReader.BaseStream.Position;

                try
                {
                    if (asMessages)
                    {
                        if (!readMessageData(binaryReader, blockHeader.capturedLen, blockHeader.tsLow,
                            blockHeader.tsHigh, results, incompletePacketMap))
                            break;
                    }
                    else
                    {
                        var packetRecord = readPacketData(binaryReader, blockHeader.capturedLen, blockHeader.tsLow,
                            blockHeader.tsHigh, curPacket);

                        if (packetRecord == null)
                            break;

                        results.Add(packetRecord);
                    }

                    curPacket++;
                }
                catch (Exception)
                {
                    binaryReader.BaseStream.Position +=
                        blockHeader.capturedLen - (binaryReader.BaseStream.Position - packetStartPos);
                }

                binaryReader.BaseStream.Position +=
                    blockHeader.blockTotalLength - (binaryReader.BaseStream.Position - blockStartPos);
            }

            return results;
        }

        private static bool readNetworkHeaders(BinaryReader binaryReader)
        {
            EthernetHeader ethernetHeader = EthernetHeader.read(binaryReader);

            // Skip non-IP packets
            if (ethernetHeader.proto != 8)
            {
                throw new InvalidDataException();
            }

            IpHeader ipHeader = IpHeader.read(binaryReader);

            // Skip non-UDP packets
            if (ipHeader.proto != 17)
            {
                throw new InvalidDataException();
            }

            UdpHeader udpHeader = UdpHeader.read(binaryReader);

            bool isSend = (udpHeader.dPort >= 9000 && udpHeader.dPort <= 9013);
            bool isRecv = (udpHeader.sPort >= 9000 && udpHeader.sPort <= 9013);

            // Skip non-AC-port packets
            if (!isSend && !isRecv)
            {
                throw new InvalidDataException();
            }

            return isSend;
        }

        private static PacketRecord readPacketData(BinaryReader binaryReader, long len, uint ts1, uint ts2,
            int curPacket)
        {
            // Begin reading headers
            long packetStartPos = binaryReader.BaseStream.Position;

            bool isSend = readNetworkHeaders(binaryReader);

            long headersSize = binaryReader.BaseStream.Position - packetStartPos;

            // Begin reading non-header packet content
            StringBuilder packetHeadersStr = new StringBuilder();
            StringBuilder packetTypeStr = new StringBuilder();

            PacketRecord packet = new PacketRecord()
            {
                index = curPacket,
                isSend = isSend,
                extraInfo = "",
                data = binaryReader.ReadBytes((int) (len - headersSize))
            };

            if (IsPcapPng)
            {
                packet.tsLow = ts1;
                packet.tsHigh = ts2;
            }
            else
            {
                packet.tsSec = ts1;
                packet.tsUsec = ts2;
            }


            using (BinaryReader packetReader = new BinaryReader(new MemoryStream(packet.data)))
            {
                try
                {
                    ProtoHeader pHeader = ProtoHeader.read(packetReader);

                    packet.optionalHeadersLen = readOptionalHeaders(pHeader.header_, packetHeadersStr, packetReader);

                    if (packetReader.BaseStream.Position == packetReader.BaseStream.Length)
                        packetTypeStr.Append("<Header Only>");

                    uint HAS_FRAGS_MASK = 0x4; // See SharedNet::SplitPacketData

                    if ((pHeader.header_ & HAS_FRAGS_MASK) != 0)
                    {
                        while (packetReader.BaseStream.Position != packetReader.BaseStream.Length)
                        {
                            if (packetTypeStr.Length != 0)
                                packetTypeStr.Append(" + ");

                            BlobFrag newFrag = readFragment(packetReader);
                            packet.frags.Add(newFrag);
                            packet.queueID = newFrag.memberHeader_.queueID;

                            if (newFrag.memberHeader_.blobNum != 0)
                            {
                                packetTypeStr.Append("FragData[");
                                packetTypeStr.Append(newFrag.memberHeader_.blobNum);
                                packetTypeStr.Append("]");
                            }
                            else
                            {
                                using (BinaryReader fragDataReader = new BinaryReader(new MemoryStream(newFrag.dat_)))
                                {
                                    PacketOpcode opcode = Util.readOpcode(fragDataReader);
                                    packet.opcodes.Add(opcode);
                                    packetTypeStr.Append(opcode);
                                }
                            }
                        }
                    }

                    if (packetReader.BaseStream.Position != packetReader.BaseStream.Length)
                        packet.extraInfo = "Didnt read entire packet! " + packet.extraInfo;
                }
                catch (OutOfMemoryException)
                {
                    //MessageBox.Show("Out of memory (packet " + curPacket + "), stopping read: " + e);
                    return null;
                }
                catch (Exception e)
                {
                    packet.extraInfo += "EXCEPTION: " + e.Message + " " + e.StackTrace;
                }
            }

            packet.packetHeadersStr = packetHeadersStr.ToString();
            packet.packetTypeStr = packetTypeStr.ToString();

            return packet;
        }

        private static bool readMessageData(BinaryReader binaryReader, long len, uint ts1, uint ts2,
            List<PacketRecord> results, Dictionary<ulong, PacketRecord> incompletePacketMap)
        {
            // Begin reading headers
            long packetStartPos = binaryReader.BaseStream.Position;

            bool isSend = readNetworkHeaders(binaryReader);

            long headersSize = binaryReader.BaseStream.Position - packetStartPos;

            // Begin reading non-header packet content
            StringBuilder packetHeadersStr = new StringBuilder();
            StringBuilder packetTypeStr = new StringBuilder();

            PacketRecord packet = null;
            byte[] packetData = binaryReader.ReadBytes((int) (len - headersSize));
            using (BinaryReader packetReader = new BinaryReader(new MemoryStream(packetData)))
            {
                try
                {
                    ProtoHeader pHeader = ProtoHeader.read(packetReader);

                    uint HAS_FRAGS_MASK = 0x4; // See SharedNet::SplitPacketData

                    if ((pHeader.header_ & HAS_FRAGS_MASK) != 0)
                    {
                        readOptionalHeaders(pHeader.header_, packetHeadersStr, packetReader);

                        while (packetReader.BaseStream.Position != packetReader.BaseStream.Length)
                        {
                            BlobFrag newFrag = readFragment(packetReader);

                            ulong blobID = newFrag.memberHeader_.blobID;
                            if (incompletePacketMap.ContainsKey(blobID))
                            {
                                packet = incompletePacketMap[newFrag.memberHeader_.blobID];
                            }
                            else
                            {
                                packet = new PacketRecord();
                                incompletePacketMap.Add(blobID, packet);
                            }

                            if (newFrag.memberHeader_.blobNum == 0)
                            {
                                packet.isSend = isSend;
                                if (IsPcapPng)
                                {
                                    packet.tsLow = ts1;
                                    packet.tsHigh = ts2;
                                }
                                else
                                {
                                    packet.tsSec = ts1;
                                    packet.tsUsec = ts2;
                                }

                                packet.extraInfo = "";

                                using (BinaryReader fragDataReader = new BinaryReader(new MemoryStream(newFrag.dat_)))
                                {
                                    PacketOpcode opcode = Util.readOpcode(fragDataReader);
                                    packet.opcodes.Add(opcode);
                                    packet.packetTypeStr = opcode.ToString();
                                }
                            }

                            packet.packetHeadersStr += packetHeadersStr.ToString();

                            packet.frags.Add(newFrag);

                            if (addPacketIfFinished(results, packet))
                            {
                                incompletePacketMap.Remove(blobID);
                            }
                        }

                        if (packetReader.BaseStream.Position != packetReader.BaseStream.Length)
                            packet.extraInfo = "Didnt read entire packet! " + packet.extraInfo;
                    }
                }
                catch (OutOfMemoryException)
                {
                    //MessageBox.Show("Out of memory (packet " + curPacket + "), stopping read: " + e);
                    return false;
                }
                catch (Exception e)
                {
                    packet.extraInfo += "EXCEPTION: " + e.Message + " " + e.StackTrace;
                }
            }

            return true;
        }

        private static BlobFrag readFragment(BinaryReader packetReader)
        {
            BlobFrag newFrag = new BlobFrag();
            newFrag.memberHeader_ = BlobFragHeader_t.read(packetReader);
            newFrag.dat_ = packetReader.ReadBytes(newFrag.memberHeader_.blobFragSize - 16); // 16 == size of frag header

            return newFrag;
        }

        private static int readOptionalHeaders(uint header_, StringBuilder packetHeadersStr, BinaryReader packetReader)
        {
            long readStartPos = packetReader.BaseStream.Position;

            if ((header_ & CServerSwitchStructHeader.mask) != 0)
            {
                /*CServerSwitchStruct serverSwitchStruct = */
                CServerSwitchStruct.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Server Switch");
            }

            if ((header_ & LogonServerAddrHeader.mask) != 0)
            {
                /*sockaddr_in serverAddr = */
                sockaddr_in.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Logon Server Addr");
            }

            if ((header_ & CEmptyHeader1.mask) != 0)
            {
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Empty Header 1");
            }

            if ((header_ & CReferralStructHeader.mask) != 0)
            {
                /*CReferralStruct referralStruct = */
                CReferralStruct.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Referral");
            }

            if ((header_ & NakHeader.mask) != 0)
            {
                /*CSeqIDListHeader nakSeqIDs = */
                NakHeader.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Nak");
            }

            if ((header_ & EmptyAckHeader.mask) != 0)
            {
                /*CSeqIDListHeader ackSeqIDs = */
                EmptyAckHeader.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Empty Ack");
            }

            if ((header_ & PakHeader.mask) != 0)
            {
                /*PakHeader pakHeader = */
                PakHeader.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Pak");
            }

            if ((header_ & CEmptyHeader2.mask) != 0)
            {
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Empty Header 2");
            }

            if ((header_ & CLogonHeader.mask) != 0)
            {
                CLogonHeader.HandshakeWireData handshakeData = CLogonHeader.HandshakeWireData.read(packetReader);
                /*byte[] authData = */
                packetReader.ReadBytes((int) handshakeData.cbAuthData);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Logon");
            }

            if ((header_ & ULongHeader.mask) != 0)
            {
                /*ULongHeader ulongHeader = */
                ULongHeader.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("ULong 1");
            }

            if ((header_ & CConnectHeader.mask) != 0)
            {
                /*CConnectHeader.HandshakeWireData handshakeData = */
                CConnectHeader.HandshakeWireData.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Connect");
            }

            if ((header_ & ULongHeader2.mask) != 0)
            {
                /*ULongHeader2 ulongHeader = */
                ULongHeader2.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("ULong 2");
            }

            if ((header_ & NetErrorHeader.mask) != 0)
            {
                /*NetError netError = */
                NetError.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Net Error");
            }

            if ((header_ & NetErrorHeader_cs_DisconnectReceived.mask) != 0)
            {
                /*NetError netError = */
                NetError.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Net Error Disconnect");
            }

            if ((header_ & CICMDCommandStructHeader.mask) != 0)
            {
                /*CICMDCommandStruct icmdStruct = */
                CICMDCommandStruct.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("ICmd");
            }

            if ((header_ & CTimeSyncHeader.mask) != 0)
            {
                /*CTimeSyncHeader timeSyncHeader = */
                CTimeSyncHeader.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Time Sync");
            }

            if ((header_ & CEchoRequestHeader.mask) != 0)
            {
                /*CEchoRequestHeader echoRequestHeader = */
                CEchoRequestHeader.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Echo Request");
            }

            if ((header_ & CEchoResponseHeader.mask) != 0)
            {
                /*CEchoResponseHeader.CEchoResponseHeaderWireData echoResponseData = */
                CEchoResponseHeader.CEchoResponseHeaderWireData.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Echo Response");
            }

            if ((header_ & CFlowStructHeader.mask) != 0)
            {
                /*CFlowStruct flowStruct = */
                CFlowStruct.read(packetReader);
                if (packetHeadersStr.Length != 0)
                    packetHeadersStr.Append(" | ");
                packetHeadersStr.Append("Flow");
            }

            return (int) (packetReader.BaseStream.Position - readStartPos);
        }
    }
}
