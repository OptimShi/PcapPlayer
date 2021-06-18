using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using ACE.PcapReader;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages;

namespace ACE.Server.Network
{
    partial class Session
    {
        private System.Timers.Timer pcapTimer;
        private uint PcapSeconds = 0;
        private ushort ForcePositionTimestamp = 0;
        private int TotalRecords = 0;
        private int PausedRecord = 0; // Used to tell if we have advanced during a "pause" or not

        private float PercentComplete = 0;
        public bool PcapPaused = false;

        /// <summary>
        /// Being the playback. This is called when the player hits "Enter World" on the client.
        /// </summary>
        public void InitPcapPlayback()
        {
            PCapReader.CurrentPcapRecordStart = PCapReader.StartRecordIndex;

            TotalRecords = PCapReader.EndRecordIndex - PCapReader.StartRecordIndex;

            // set and start our timer
            pcapTimer = new System.Timers.Timer(1000);
            pcapTimer.Elapsed += OnPcapTimer;
            pcapTimer.AutoReset = true;
            pcapTimer.Enabled = true;
            PcapPaused = false;
        }

        public void JumpToTime()
        {

        }


        public void PausePcapPlayback()
        {
            if (pcapTimer != null && pcapTimer.Enabled)
            {
                pcapTimer.Stop();
                pcapTimer.Enabled = false;
                PcapPaused = true;
                Console.WriteLine("Pcap Playback Has Paused.");
            }
        }

        public void RestartPcapPlayback()
        {
            if (pcapTimer != null && pcapTimer.Enabled == false)
            {
                
                if(PCapReader.CurrentPcapRecordStart != PausedRecord)
                {
                    // We need to adjust the PcapSeconds timer, as we have lept around in time
                    PcapSeconds = PCapReader.Records[PCapReader.CurrentPcapRecordStart].tsSec - PCapReader.StartTime - 1;
                }

                pcapTimer.Enabled = true;
                pcapTimer.Start();
                PcapPaused = false;
                Console.WriteLine("Pcap Playback Has Restarted.");
            }
        }

        public void StopPcapPlayback()
        {
            if (pcapTimer != null && pcapTimer.Enabled)
            {
                pcapTimer.Stop();
                pcapTimer.Dispose();
                Console.WriteLine("Pcap Playback Has Stopped.");
                PcapPaused = false;
                PausedRecord = PCapReader.CurrentPcapRecordStart;
            }
        }

        /// <summary>
        /// Resumes the pcap playback once the client reports that login is complete
        /// </summary>
        public void PcapLoginComplete()
        {
            PCapReader.CurrentPcapRecordStart = PCapReader.PausedRecordIndex;
        }

        private void OnPcapTimer(Object source, ElapsedEventArgs e)
        {
            // Console.WriteLine("The Elapsed event of the pcapTimer was raised at {0:HH:mm:ss.fff}", e.SignalTime);
            uint myTimer = PCapReader.StartTime + PcapSeconds;
            for (var i = PCapReader.CurrentPcapRecordStart; i < PCapReader.EndRecordIndex; i++)
            {
                // Check if the record is not a "send" (client TO server)
                if (PCapReader.Records[i].isSend == false && PCapReader.Records[i].tsSec == myTimer)
                {
                    GameMessageGroup group = GameMessageGroup.InvalidQueue;
                    if (PCapReader.Records[i].frags.Count > 0)
                    {
                        group = (GameMessageGroup)PCapReader.Records[i].frags[0].memberHeader_.queueID;
                    }

                    var opcode = (GameMessageOpcode)PCapReader.Records[i].opcodes[0];
                    // Console.WriteLine("--Sending packet " + opcode.ToString());
                    // Set opcode to none since it will be written as part of the data packet
                    GameMessage newMessage = new GameMessage(GameMessageOpcode.None, group);
                    switch (opcode)
                    {
                        case GameMessageOpcode.UpdatePosition:
                            var updatePosData = getUpdatePosition(PCapReader.Records[i].data);
                            newMessage.Writer.Write(updatePosData);
                            break;
                        case GameMessageOpcode.MovementEvent:
                            var movementEventData = getMovementEvent(PCapReader.Records[i].data);
                            newMessage.Writer.Write(movementEventData);
                            break;
                        default:
                            newMessage.Writer.Write(PCapReader.Records[i].data);
                            break;
                    }

                    SendMessage(newMessage);
                    PCapReader.CurrentPcapRecordStart = i;
                }
            }
            PcapSeconds++;

            // Write out how far along we are...
            var perc = (float)PCapReader.CurrentPcapRecordStart / (float)TotalRecords * 100f;
            string percentDone = perc.ToString("0.0");
            if (percentDone != PercentComplete.ToString("0.0"))
            {
                Console.WriteLine($"Processed record {PCapReader.CurrentPcapRecordStart} - {percentDone}% complete.");
                PercentComplete = perc;
            }

            if (PCapReader.Records[PCapReader.EndRecordIndex - 1].tsSec <= myTimer)
            {
                Console.WriteLine("*************** FINISHED ***************");
                StopPcapPlayback();
            }
        }

        private byte[] getUpdatePosition(byte[] data)
        {
            string s_guid = "0x" +
                data[7].ToString("X2") +
                data[6].ToString("X2") +
                data[5].ToString("X2") +
                data[4].ToString("X2");
            uint guid = Convert.ToUInt32(s_guid, 16);
            if(guid == PCapReader.CharacterGUID)
            {
                byte[] bytes = BitConverter.GetBytes(ForcePositionTimestamp);
                data[data.Length - 2] = bytes[0];
                data[data.Length - 1] = bytes[1];
                ForcePositionTimestamp++;
                // Console.WriteLine($"  --  ForcePositionTimestamp: {ForcePositionTimestamp}");
            }
            return data;
        }

        private byte[] getMovementEvent(byte[] data)
        {
            string s_guid = "0x" +
                data[7].ToString("X2") +
                data[6].ToString("X2") +
                data[5].ToString("X2") +
                data[4].ToString("X2");
            uint guid = Convert.ToUInt32(s_guid, 16);
            if (guid == PCapReader.CharacterGUID)
            {
                data[14] = 0;
                // Console.WriteLine($"  --  MovementEvent");
            }
            return data;
        }

    }

}
