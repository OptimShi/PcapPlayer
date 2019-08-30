using System;
using System.Collections.Generic;

using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Server.Network;
using ACE.PcapReader;

namespace ACE.Server.Command.Handlers
{
    public static class ConsoleCommands
    {
        [CommandHandler("pcap-load", AccessLevel.Player, CommandHandlerFlag.ConsoleInvoke, 0, "Load a PCAP for playback.", "<full-path-to-pcap-file>")]
        public static void HandleLoadPcap(Session session, params string[] parameters)
        {
            // pcap-load "D:\Asheron's Call\Log Files\PCAP Part 1\AC1Live-The-Ripley-Collection-part01\AC1Live-The-Ripley-Collection\aclog pcaps\pkt_2017-1-9_1484023507_log.pcap"
            // pcap-load "D:\ACE\Logs\PCAP Part 1\AC1Live-The-Ripley-Collection-part01\AC1Live-The-Ripley-Collection\aclog pcaps\pkt_2017-1-9_1484023507_log.pcap"

            string pcapFileName;
            if (parameters?.Length != 1)
            {
                Console.WriteLine("pcap-load <full-path-to-pcap-file>");
                //pcapFileName = "D:\\ACE\\player.pcap"; // 
                return;
            }
            else
            {
                pcapFileName = parameters[0];
            }

            // Check if file exists!
            if (!System.IO.File.Exists(pcapFileName))
            {
                Console.WriteLine("Could not find pcap file to load: " + pcapFileName);
                return;
            }
            bool abort = false;

            Console.WriteLine($"Loading pcap...");

            //List<PacketRecord> records = PCapReader.LoadPcap(pcapFileName, true, ref abort);
            PCapReader.LoadPcap(pcapFileName, true, ref abort);

            Console.WriteLine($"Pcap Loaded with " + PCapReader.Records.Count.ToString() + " records.");

            if (PCapReader.LoginInstances > 0)
            {
                Console.WriteLine("\n" + PCapReader.LoginInstances.ToString() + " unique login events detected.");
                if (PCapReader.LoginInstances > 1)
                    Console.WriteLine("Please specify a login to use using the comamnd 'pcap-login <login-#>', where <login-#> is 1 to " + PCapReader.LoginInstances.ToString() + "\n");
                Console.WriteLine("Login set to first instance.");

                Console.WriteLine("StartRecordIndex: " + PCapReader.StartRecordIndex);
                Console.WriteLine("EndRecordIndex: " + PCapReader.EndRecordIndex);
            }
            else
            {
                Console.WriteLine("\nNo login events detected. We cannot play back this pcap at this time, sorry.\n");
            }

            Console.WriteLine("");
        }

        [CommandHandler("pcap-login", AccessLevel.Player, CommandHandlerFlag.ConsoleInvoke, 0, "Specify a login instance for pcap playback.", "<login-#>")]
        public static void HandlePcapSetLogin(Session session, params string[] parameters)
        {
            if (parameters?.Length != 1)
            {
                if (PCapReader.LoginInstances > 0)
                {
                    Console.WriteLine("\n" + PCapReader.LoginInstances.ToString() + " unique login events detected. Please specify a login to use using the comamnd 'pcap-login <login-#>', where <login-#> is 1 to " + PCapReader.LoginInstances.ToString() + "\n");
                }
                else
                {
                    Console.WriteLine("\nNo login events detected. We cannot play back this pcap at this time, sorry.\n");
                }
                return;
            }

            if (int.TryParse(parameters[0], out int loginID))
            {
                PCapReader.SetLoginInstance(loginID);
                Console.WriteLine("Login instance set. Pcap will play records " + PCapReader.StartRecordIndex.ToString() + " to " + PCapReader.EndRecordIndex.ToString());
                PCapReader.GetPcapDuration();
            }
            else
            {
                Console.WriteLine("Unable to set login instance.");
            }

        }
    }
}
