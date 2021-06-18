namespace ACE.Common
{
    public class MasterConfiguration
    {
        public GameConfiguration Server { get; set; }

        public DatabaseConfiguration MySql { get; set; }

        public MasterConfiguration()
        {
        }

        /// <summary>
        /// This will load some default values in case we're not using a config file, which contains 99% useless stuff for our PCAP playing needs
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public MasterConfiguration(string host, uint port)
        {
            Server = new GameConfiguration();
            Server.Network = new NetworkSettings();
            Server.Network.Host = host;
            Server.Network.Port = port;
            Server.Network.MaximumAllowedSessions = 1;
            Server.Network.DefaultSessionTimeout = 90;
        }
    }
}
