using System;
using System.Collections.Generic;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Server.Network;
using ACE.PcapReader;
using ACE.Entity;
using PcapPlayer.Entity;
using System.Linq;

namespace ACE.Server.Command.Handlers
{
    public static class ConsoleCommands
    {

        // acecommands
        [CommandHandler("help", AccessLevel.Player, CommandHandlerFlag.None, 0, "Lists all commands.", "<access level or search>")]
        public static void HandleHelp(Session session, params string[] parameters)
        {
            var commandList = new List<string>();

            var msgHeader = "Note: You may substitute a forward slash (/) for the at symbol (@).\n"
                          + "For more information, type @acehelp < command >.\n";

            if (session == null)
                Console.WriteLine("For more information, type acehelp < command >.");

            var accessLevel = session != null ? session.AccessLevel : AccessLevel.Admin;
            var exact = false;
            string search = null;

            if (parameters.Length > 0)
            {
                var param = parameters[0];
                if (Enum.TryParse(param, true, out AccessLevel pAccessLevel) && pAccessLevel <= accessLevel)
                {
                    accessLevel = pAccessLevel;
                    exact = true;
                }
                else
                    search = param;
            }

            var restrict = session != null ? CommandHandlerFlag.ConsoleInvoke : CommandHandlerFlag.RequiresWorld;

            var commands = from cmd in CommandManager.GetCommands()
                           where (exact ? cmd.Attribute.Access == accessLevel : cmd.Attribute.Access <= accessLevel) && cmd.Attribute.Flags != restrict
                           && (search != null ? $"{cmd.Attribute.Access} {cmd.Attribute.Command} {cmd.Attribute.Description}".Contains(search, StringComparison.OrdinalIgnoreCase) : true)
                           orderby cmd.Attribute.Command
                           select cmd;

            foreach (var command in commands)
                commandList.Add(string.Format("@{0} - {1}", command.Attribute.Command, command.Attribute.Description));

            var msg = string.Join("\n", commandList);
            Console.WriteLine(msg);
        }

        [CommandHandler("pcap-load", AccessLevel.Player, CommandHandlerFlag.ConsoleInvoke, 0,
            "Load a PCAP for playback.", "<full-path-to-pcap-file>")]
        public static void HandleLoadPcap(Session session, params string[] parameters)
        {
            // pcap-load "D:\Asheron's Call\Log Files\PCAP Part 1\AC1Live-The-Ripley-Collection-part01\AC1Live-The-Ripley-Collection\aclog pcaps\pkt_2017-1-9_1484023507_log.pcap"
            // pcap-load "D:\ACE\Logs\PCAP Part 1\AC1Live-The-Ripley-Collection-part01\AC1Live-The-Ripley-Collection\aclog pcaps\pkt_2017-1-9_1484023507_log.pcap"

            string pcapFileName;
            if (parameters?.Length != 1)
            {
                Console.WriteLine("pcap-load <full-path-to-pcap-file>");
                return;
            }

            pcapFileName = parameters[0];

            // Check if file exists!
            if (!System.IO.File.Exists(pcapFileName))
            {
                Console.WriteLine($"Could not find pcap file to load: {pcapFileName}");
                return;
            }

            bool abort = false;
            Console.WriteLine($"Loading pcap...");

            //List<PacketRecord> records = PCapReader.LoadPcap(pcapFileName, true, ref abort);
            PCapReader.LoadPcap(pcapFileName, true, ref abort);

            Console.WriteLine($"Pcap Loaded with {PCapReader.Records.Count} records.");

            if (PCapReader.LoginInstances > 0)
            {
                Console.WriteLine($"\n{PCapReader.LoginInstances} unique login events detected.");
                if (PCapReader.LoginInstances > 1)
                    Console.WriteLine($"Please specify a login to use using the command 'pcap-login <login-#>', where <login-#> is 1 to {PCapReader.LoginInstances}\n");
                Console.WriteLine("Login set to first instance.");

                if (PCapReader.TeleportIndexes.ContainsKey(1))
                {
                    Console.WriteLine($"Instance has {PCapReader.TeleportIndexes[1].Count} teleports. Use @teleport in-game to advance to next, or @teleport <index> to select a specific one.");
                    Console.WriteLine($"\nUse `list` to display a detailed breackdown of all login instances and teleports.\n");
                }
                else
                    Console.WriteLine($"Instance has no teleports.");

                Console.WriteLine($"StartRecordIndex: {PCapReader.StartRecordIndex}");
                Console.WriteLine($"EndRecordIndex: {(PCapReader.EndRecordIndex - 1)}");
            }
            else
            {
                Console.WriteLine("\nNo login events detected. We will attempt to join this pcap already in progress.\n");
                if (PCapReader.TeleportIndexes.ContainsKey(0))
                {
                    Console.WriteLine($"Instance has {PCapReader.TeleportIndexes[0].Count} teleports. Use @teleport in-game to advance to next, or @teleport <index> to select a specific one.");
                    Console.WriteLine($"\nUse `list` to display a detailed breackdown of teleports.\n");

                }
                else
                    Console.WriteLine($"Instance has no teleports.");
            }

            var port = Common.ConfigManager.Config.Server.Network.Port;
            Console.WriteLine($"\nTo connect, enter the following command at the Command Prompt in your Asheron's Call folder, or use a launcher to connect using any username and password combination.\n\n    acclient.exe -h 127.0.0.1:{port} -a USER -v PASS\n");
        }

        [CommandHandler("pcap-login", AccessLevel.Player, CommandHandlerFlag.ConsoleInvoke, 0,
            "Specify a login instance for pcap playback.", "<login-#>")]
        public static void HandlePcapSetLogin(Session session, params string[] parameters)
        {
            if (parameters?.Length != 1)
            {
                if (PCapReader.LoginInstances > 0)
                {
                    Console.WriteLine(
                        $"\n{PCapReader.LoginInstances} unique login events detected. Please specify a login to use using the command 'pcap-login <login-#>', where <login-#> is 1 to {PCapReader.LoginInstances}\n");
                }
                else
                {
                    Console.WriteLine(
                        "\nNo login events detected. We cannot play back this pcap at this time, sorry.\n");
                }

                return;
            }

            if (int.TryParse(parameters[0], out int loginID))
            {
                PCapReader.SetLoginInstance(loginID);
                Console.WriteLine(
                    $"Login instance set. Pcap will play records {PCapReader.StartRecordIndex}  to {PCapReader.EndRecordIndex - 1}");
                Console.WriteLine(
                    $"Instance has {PCapReader.TeleportIndexes[loginID].Count} teleports. Use @teleport in-game to advance to next, or @teleport <index> to select a specific one.");
                PCapReader.GetPcapDuration();
            }
            else
            {
                Console.WriteLine("Unable to set login instance.");
            }
        }

        [CommandHandler("teleport", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
            "Sends player to next teleport instance in Pcap", "")]
        public static void HandleTeleport(Session session, params string[] parameters)
        {
            session.PausePcapPlayback();
            int? teleportID = null;
            if (parameters?.Length > 0)
            {
                // If we fail to get a valid int, we will continue with null (which means "next instance");
                if (int.TryParse(parameters[0], out int teleportIndex))
                    teleportID = teleportIndex;
            }

            bool teleportFound = PCapReader.DoTeleport(teleportID);
            if (teleportFound)
                Console.WriteLine($"Advancing to next teleport session, entry {PCapReader.CurrentPcapRecordStart}");
            else
                Console.WriteLine("Sorry, there were no additional teleport events in this pcap.");
            session.RestartPcapPlayback();
        }

        // Old function, no longer used. Left fo
        public static void HandleMarkerList(Session session, params string[] parameters)
        {
            if (parameters?.Length > 0)
            {
                Console.WriteLine("This command doesn't take parameters.");
            }

            if (PCapReader.PcapMarkers.Count > 0)
            {
                var teleportIndex = 0;
                foreach (var pcapMarker in PCapReader.PcapMarkers)
                {
                    if (pcapMarker.Type == MarkerType.Login)
                    {
                        Console.WriteLine($"Player Login {pcapMarker.LoginInstance}: line {pcapMarker.LineNumber}");
                        teleportIndex = 0;
                    }
                    else if (pcapMarker.Type == MarkerType.Teleport)
                    {
                        Console.WriteLine(
                            $"  Teleport {teleportIndex + 1}: line {pcapMarker.LineNumber}");
                        teleportIndex++;
                    }
                }

                Console.WriteLine($"End of pcap: line {PCapReader.EndRecordIndex - 1}");
            }
            else
            {
                Console.WriteLine("Sorry, there are no login or teleport events in this pcap.");
            }
        }

        [CommandHandler("markerlist", AccessLevel.Player, CommandHandlerFlag.ConsoleInvoke, 0, "Alias of `list`.", "")]

        [CommandHandler("list", AccessLevel.Player, CommandHandlerFlag.ConsoleInvoke, 0, "Lists the teleport locations, timestamps, and line numbers in the currently selected pcap.", "")]
        public static void HandleTeleportList(Session session, params string[] parameters)
        {
            if (PCapReader.PcapMarkers.Count > 0)
            {
                Console.WriteLine("Help: [Type] [instance], [pcap line number], [timespan from login HH:MM:ss] - [Approx Location]");
                DungeonList dungeons = new DungeonList();
                var teleportIndex = 0;
                for(var i = 0; i < PCapReader.PcapMarkers.Count; i++)
                {
                    var pcapMarker = PCapReader.PcapMarkers[i];
                    var line = pcapMarker.LineNumber;

                    CM_Movement.Position? pos;
                    if ((i + 1) < PCapReader.PcapMarkers.Count)
                    {
                        pos = PCapReader.GetDetailedLocationInfo(line, PCapReader.PcapMarkers[i + 1].LineNumber);
                    }
                    else
                    {
                        pos = PCapReader.GetDetailedLocationInfo(line, PCapReader.EndRecordIndex);
                    }

                    // Set default as "unknown"
                    string loc = "Unable to determine location.";
                    if (pos != null)
                    {
                        // convert the "Position" to a "Position"
                        var acePos = new Position(
                            pos.objcell_id,
                            new System.Numerics.Vector3(pos.x, pos.y, pos.z),
                            new System.Numerics.Quaternion(pos.qw, pos.qx, pos.qy, pos.qz)
                            );
                        var coords = Entity.PositionExtensions.GetMapCoordStr(acePos);
                        if (coords != null)
                        {
                            loc = coords;
                        }
                        else
                        {
                            // Are we in a dungeon?
                            if ((pos.objcell_id & 0xFFFF) >= 0x100)
                            {
                                var landblock = pos.objcell_id >> 16;
                                loc = $"Unable to determine dungeon location (0x{landblock:X4}).";
                                string dungeonName = dungeons.GetDungeonName(landblock);
                                if (dungeonName != "")
                                    loc = dungeonName;
                            }
                        }
                    }

                    
                    switch (pcapMarker.Type)
                    {
                        case MarkerType.Login:
                            //Console.WriteLine($"Login/Initial Position: {loc}");
                            Console.WriteLine($"Player Login {pcapMarker.LoginInstance}, line {pcapMarker.LineNumber} - {loc}");
                            teleportIndex = 1;
                            break;
                        case MarkerType.Teleport:
                            string time = PCapReader.GetPcapTime(pcapMarker);
                            Console.WriteLine($"  Teleport {teleportIndex++}, line {pcapMarker.LineNumber}, {time} - {loc}");
                            break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Sorry, there are no login or teleport events in this pcap.");
            }
            Console.WriteLine();
        }

        [CommandHandler("pause", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0, "Pause or unpause Pcap playback", "")]
        public static void HandlePause(Session session, params string[] parameters)
        {
            if (!session.PcapPaused)
                session.PausePcapPlayback();
            else
                session.RestartPcapPlayback();
        }


        [CommandHandler("timewarp", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1, "Jump forward or backward in time, relative to current location.", "")]
        public static void HandleTimewarp(Session session, params string[] parameters)
        {
            if (parameters?.Length > 0)
            {
                // If we fail to get a valid int, we will continue with null (which means "next instance");
                if (float.TryParse(parameters[0], out float timeOffset))
                {
                    if(timeOffset > 0)
                        Console.WriteLine($"\nAttempting to jump forward by {timeOffset} minutes.");
                    else
                        Console.WriteLine($"\nAttempting to jump backward by {timeOffset} minutes.");

                    Console.WriteLine("\nNote that using this function can cause the client to miss out on the CreateObject message or certain items and may cause them to be invisible.");
                }
            }


        }

        [CommandHandler("goto", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 1, "Sets the pcap to a specific time.", "goto <time>, e.g. 'goto 90', in seconds, or 'goto 1:30'")]
        public static void HandleGoTo(Session session, params string[] parameters)
        {
            if (parameters?.Length != 1)
            {
                Console.WriteLine("Usage: goto <time>");
                Console.WriteLine("  Examples:");
                Console.WriteLine("     goto 90 - will jump to the 90 second mark");
                Console.WriteLine("     goto 1:30 - will jump to the 90 second mark");
                Console.WriteLine("     goto 5400 - will jump to the 90 minute mark");
                Console.WriteLine("     goto 1:30:00 - will jump to the 90 minute mark");
                return;
            }

            int time_goto = 0;
            // Check if parameter has a colon for a timestamp
            if (parameters[0].Contains(":"))
            {
                time_goto = ConvertMyTimestamp(parameters[0]);
                if (time_goto <= 0)
                {
                    Console.WriteLine("Please specify a positive time value.");
                    return;
                }
            }
            else
            {
                if (int.TryParse(parameters[0], out int timeTemp) && timeTemp > 0)
                {
                    time_goto = timeTemp;
                }
                else
                {
                    Console.WriteLine("Please specify a valid, positive time value.");
                    return;
                }
            }


        }

        /// <summary>
        /// Converts a user inputted timestamp in hh:mm:ss or mm:ss to just seconds.
        /// There's probably a nicer built-in function for this, but this also works.
        /// </summary>
        /// <param name="timestmap"></param>
        /// <returns></returns>
        public static int ConvertMyTimestamp(string timestmap)
        {
            int myTime = 0;
            string[] timeunits = timestmap.Split(':');
            if (timeunits.Length > 3)
            {
                Console.WriteLine("Please use 'hh:mm:ss' format.");
                return 0;
            }

            for (var i = 0; i < timeunits.Length; i++)
            {
                // gets our time unit multipler, 60 * 60 for hours, just *60 for minutes
                int exp = timeunits.Length - 1 - i;
                int multi = 1;
                switch (exp)
                {
                    case 2: multi = 60 * 60; break;
                    case 1: multi = 60; break;
                    case 0: multi = 1; break;
                }
                if (int.TryParse(timeunits[i], out int timeTemp))
                {
                    myTime += multi * timeTemp;
                }
                else
                {
                    Console.WriteLine("Please use 'hh:mm:ss' format.");
                    return 0;
                }
            }

            return myTime;
        }
    }
}
