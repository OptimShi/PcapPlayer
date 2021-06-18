using System;
using System.Collections.Generic;
using System.Text;

namespace ACE.PcapReader
{
    public class PcapMarker
    {
        public int LoginInstance;
        public int LineNumber;
        public MarkerType Type;

        public PcapMarker(MarkerType type, int lineNumber, int loginInstance)
        {
            Type = type;
            LineNumber = lineNumber;
            LoginInstance = loginInstance;
        }
    }
    public enum MarkerType
    {
        Login,
        Teleport
    }
}
