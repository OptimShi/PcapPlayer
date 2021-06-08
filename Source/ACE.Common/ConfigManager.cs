using System;
using System.IO;

using Newtonsoft.Json;
using DouglasCrockford.JsMin;

namespace ACE.Common
{
    public static class ConfigManager
    {
        public static MasterConfiguration Config { get; private set; }

        /// <summary>
        /// initializes from a preloaded configuration
        /// </summary>
        public static void Initialize(MasterConfiguration configuration)
        {
            Config = configuration;
        }

        /// <summary>
        /// initializes from a Config.js file specified by the path
        /// </summary>
        public static void Initialize(string filename = @"Config.js")
        {
            var path = Path.GetDirectoryName(filename);

            if (string.IsNullOrWhiteSpace(path))
                path = Environment.CurrentDirectory;

            string fpOld = Path.Combine(path, Path.GetFileNameWithoutExtension(filename) + ".json");
            string fpNew = Path.Combine(path, Path.GetFileNameWithoutExtension(filename) + ".js");
            string fpChoice = null;
            try
            {
                if (!File.Exists(fpNew) && File.Exists(fpOld))
                {
                    File.Move(fpOld, fpNew);
                    fpChoice = fpNew;
                }
                else if (File.Exists(fpNew))
                {
                    fpChoice = fpNew;
                }
                else
                {
                    Console.WriteLine("Configuration file is missing.  Will use default settings for host and port. If you wish to change these settings, pleae copy the file Config.js.example to Config.js and edit it to match your needs");
                    Config = new MasterConfiguration("0.0.0.0", 9000);
                }

                // Config is not null if we've already created it with our default settings due to a missing config file
                if(Config == null)
                    Config = JsonConvert.DeserializeObject<MasterConfiguration>(new JsMinifier().Minify(File.ReadAllText(fpChoice)));
            }
            catch (Exception exception)
            {
                Console.WriteLine("An exception occured while loading the configuration file!");
                Console.WriteLine($"Exception: {exception.Message}");

                // environment.exit swallows this exception for testing purposes.  we want to expose it.
                throw;
            }
        }
    }
}
