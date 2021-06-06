using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PcapPlayer.Entity
{
    class DungeonList
    {
        Dictionary<uint, string> Dungeons = new Dictionary<uint, string>();

        public DungeonList()
        {
            if (Dungeons.Count > 0) return;

            // Check for landblocks.json file
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filename = appPath + "\\landblocks.json";
            if (File.Exists(filename))
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    string contents = reader.ReadToEnd();
                    Dungeons = JsonConvert.DeserializeObject<Dictionary<uint, string>>(contents);
                }
            }
        }

        public string GetDungeonName(uint objCell)
        {
            if (Dungeons.ContainsKey(objCell))
            {
                return Dungeons[objCell];
            }
            return "";
        }
    }
}
