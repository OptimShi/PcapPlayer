using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ACE.PcapReader
{
    public static partial class PCapReader
    {
        public static void FindNoLoginCharacterGUID()
        {
            Dictionary<uint, int> possibleGUIDs = new Dictionary<uint, int>();
            bool previousIsMoveToState = false;

            if (Records.Count > 0)
            {
                foreach (var record in Records)
                {
                    try
                    {
                        if (record.data.Length <= 4)
                            continue;

                        using (BinaryReader fragDataReader = new BinaryReader(new MemoryStream(record.data)))
                        {
                            PacketOpcode opcode = Util.readOpcode(fragDataReader);
                            switch (opcode)
                            {
                                case PacketOpcode.Evt_Movement__MoveToState_ID:
                                    previousIsMoveToState = true;
                                    break;
                                case PacketOpcode.Evt_Movement__MovementEvent_ID:
                                    if (previousIsMoveToState)
                                    {
                                        // fragDataReader.BaseStream.Position += 4;
                                        uint guid = fragDataReader.ReadUInt32();
                                        if (possibleGUIDs.ContainsKey(guid))
                                            possibleGUIDs[guid]++;
                                        else
                                            possibleGUIDs.Add(guid, 1);
                                    }
                                    break;
                                default:
                                    previousIsMoveToState = false;
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        // Do something with the exception maybe
                    }
                }
            }

            if (possibleGUIDs.Count > 0)
            {
                // Set to the most frequent result. This should usually be the character.
                CharacterGUID = possibleGUIDs.FirstOrDefault(x => x.Value == possibleGUIDs.Values.Max()).Key;
            }
        }

        /// <summary>
        /// Search through the loaded pcap to find the first instance of a position.
        /// We'll use that position to set the initial player location for the no-login pcap.
        /// </summary>
        public static void GetPlayerStartingPosition()
        {
            if (Records.Count > 0)
            {
                foreach (var record in Records)
                {
                    try
                    {
                        if (record.data.Length <= 4)
                            continue;

                        using (BinaryReader fragDataReader = new BinaryReader(new MemoryStream(record.data)))
                        {
                            PacketOpcode opcode = Util.readOpcode(fragDataReader);
                            switch (opcode)
                            {
                                case PacketOpcode.Evt_Movement__UpdatePosition_ID:
                                    if (ReadUpdatePosition(fragDataReader))
                                    {
                                        return;
                                    }
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        // Do something with the exception maybe
                    }
                }
            }
        }

        // Reads the Update Position and, if it matches the PlayerGUID, sets the relevant positions and returns true.
        // Otherwise, returns false
        private static bool ReadUpdatePosition(BinaryReader binaryReader)
        {
            uint object_id = binaryReader.ReadUInt32();
            if (object_id == CharacterGUID)
            {
                var bitfield = binaryReader.ReadUInt32();
                PlayerObjcell = binaryReader.ReadUInt32();
                PlayerX = binaryReader.ReadSingle();
                PlayerY = binaryReader.ReadSingle();
                PlayerZ = binaryReader.ReadSingle();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Load the basic-login.pcap. We will insert this before the pcap we are attempting to play.
        /// </summary>
        public static void LoadLoginPcap()
        {
            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "basic-login.pcap");

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    bool abort = false;
                    uint magicNumber = binaryReader.ReadUInt32();

                    binaryReader.BaseStream.Position = 0;

                    List<PacketRecord> loginRecords;
                    if (magicNumber == 0xA1B2C3D4 || magicNumber == 0xD4C3B2A1)
                    {
                        loginRecords = loadPcapPacketRecords(binaryReader, true, ref abort);
                        IsPcapPng = false;
                    }
                    else
                    {
                        loginRecords = loadPcapngPacketRecords(binaryReader, true, ref abort);
                        IsPcapPng = true;
                    }

                    FindNoLoginCharacterGUID();
                    GetPlayerStartingPosition();

                    loginRecords = UpdateLoginPcaps(loginRecords);

                    // Insert these new records before the loaded pcap, so we have our pseudo-login
                    var mergeRecords = new List<PacketRecord>(loginRecords.Count + Records.Count);
                    mergeRecords.AddRange(loginRecords);
                    mergeRecords.AddRange(Records);
                    Records = mergeRecords;
                }
            }
        }

        /// <summary>
        /// Update the Login PCaps with the proper characterGUID and location, and remove "Bad" records that can cause issues
        /// </summary>
        public static List<PacketRecord> UpdateLoginPcaps(List<PacketRecord> records)
        {
            StartRecordIndex = 7; // set the start index to this event
            StartTime = Records[0].tsSec;

            //CharacterGUID = 0x33333333;
            for (int i = 0; i < records.Count; i++)
            {
                var packet = records[i].data;
                // Update all the F7B0 - GameMessage events
                if (packet[0] == 0xB0 && packet[1] == 0xF7 && packet[2] == 0 && packet[3] == 0)
                    records[i] = UpdateGUID(records[i]);

                // Update Timestamps
                records[i] = UpdateTimestamp(records[i]);
            }

            byte[] instanceSequence = GetInstanceSequenceFromPcap();

            records[19] = UpdateGUID(records[19]); // CreatePlayer

            var createObject_Player = UpdateGUID(records[20]);
            // objcell_id
            int myPos = 252;
            createObject_Player.data[myPos++] = (byte)(PlayerObjcell & 0xFF);
            createObject_Player.data[myPos++] = (byte)(PlayerObjcell >> 8 & 0xFF);
            createObject_Player.data[myPos++] = (byte)(PlayerObjcell >> 16 & 0xFF);
            createObject_Player.data[myPos++] = (byte)(PlayerObjcell >> 24 & 0xFF);
            // PosX
            byte[] x = BitConverter.GetBytes(PlayerX);
            createObject_Player.data[myPos++] = x[0];
            createObject_Player.data[myPos++] = x[1];
            createObject_Player.data[myPos++] = x[2];
            createObject_Player.data[myPos++] = x[3];
            // PosY
            byte[] y = BitConverter.GetBytes(PlayerY);
            createObject_Player.data[myPos++] = y[0];
            createObject_Player.data[myPos++] = y[1];
            createObject_Player.data[myPos++] = y[2];
            createObject_Player.data[myPos++] = y[3];
            // PosZ
            byte[] z = BitConverter.GetBytes(PlayerZ);
            createObject_Player.data[myPos++] = z[0];
            createObject_Player.data[myPos++] = z[1];
            createObject_Player.data[myPos++] = z[2];
            createObject_Player.data[myPos++] = z[3];
            // Update Instance Sequence Timestamp
            createObject_Player.data[0x13C] = instanceSequence[0];
            createObject_Player.data[0x13D] = instanceSequence[1];
            // Save back to the records list
            records[20] = createObject_Player;

            var createObject_Robe = records[21]; // Update the WielderID
            createObject_Robe.data[0x93] = (byte)(CharacterGUID & 0xFF);
            createObject_Robe.data[0x94] = (byte)(CharacterGUID >> 8 & 0xFF);
            createObject_Robe.data[0x95] = (byte)(CharacterGUID >> 16 & 0xFF);
            createObject_Robe.data[0x96] = (byte)(CharacterGUID >> 24 & 0xFF);
            records[21] = createObject_Robe;


            // SetState
            var setState = UpdateGUID(records[33]);
            // Update Instance Sequence Timestamp
            setState.data[0xC] = instanceSequence[0];
            setState.data[0xD] = instanceSequence[1];
            records[33] = setState;

            records.RemoveAt(34); // UpdateInt - Age
            records.RemoveAt(31); // AutonomousPosition -- Remove this so it doesn't conflict with where we want to be!
            records.RemoveAt(18); // Welcome Message

            PausedRecordIndex = 29; // Index is actually 32, but we nixed 3 entries above

            return records;
        }

        private static PacketRecord UpdateGUID(PacketRecord packet)
        {
            packet.data[4] = (byte)(CharacterGUID & 0xFF);
            packet.data[5] = (byte)(CharacterGUID >> 8 & 0xFF);
            packet.data[6] = (byte)(CharacterGUID >> 16 & 0xFF);
            packet.data[7] = (byte)(CharacterGUID >> 24 & 0xFF);
            return packet;
        }

        /// <summary>
        /// Sets the timestamp on the packet to the same as the PCAP.
        /// This is used to set the Login pcap in time with the PCAP
        /// </summary>
        private static PacketRecord UpdateTimestamp(PacketRecord packet)
        {
            packet.tsHigh = Records[0].tsHigh;
            packet.tsLow = Records[0].tsLow;
            packet.tsSec = Records[0].tsSec;
            packet.tsUsec = Records[0].tsUsec;
            return packet;
        }

        private static byte[] GetInstanceSequenceFromPcap()
        {
            byte[] result = new byte[2];

            foreach (var record in Records)
            {
                var packet = record.data;
                // Look for Movement__MovementEvent_ID event.
                if (packet[0] == 0x4C && packet[1] == 0xF7 && packet[2] == 0 && packet[3] == 0
                    && packet[4] == (byte)(CharacterGUID & 0xFF)
                    && packet[5] == (byte)(CharacterGUID >> 8 & 0xFF)
                    && packet[6] == (byte)(CharacterGUID >> 16 & 0xFF)
                    && packet[7] == (byte)(CharacterGUID >> 24 & 0xFF)
                    )
                { 
                    result[0] = record.data[8];
                    result[1] = record.data[9];
                    return result;
                }
            }
            return result;
        }
    }
}
