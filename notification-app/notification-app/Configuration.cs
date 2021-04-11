using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace notification_app
{
    class Configuration
    {
        private static readonly string CONFIG_FILENAME = Path.Combine(
            Environment.GetEnvironmentVariable("LocalAppData"),
            "nullinside", "notification-app", "config.json");

        private static Configuration instance;

        public string TwitchUsername { get; set; }

        public string TwitchOauth { get; set; }

        public string TwitchChannel { get; set; }
        public string TtsVoice { get; set; }
        public uint TtsVolume { get; set; }
        public string MicrophoneGuid { get; set; }
        public bool PauseDuringSpeech { get; set; }
        public bool TTSOn { get; set; }
        public int PauseThreshold { get; set; }

        protected Configuration()
        {
        }

        public static Configuration Instance()
        {
            if (null == instance)
            {
                instance = ReadConfiguration();
            }

            return instance;
        }

        public static Configuration ReadConfiguration()
        {
            Configuration config = null;

            try
            {
                JsonSerializer serializer = new JsonSerializer();
                using (StreamReader sr = new StreamReader(CONFIG_FILENAME))
                using (JsonReader jr = new JsonTextReader(sr))
                {
                    config = serializer.Deserialize<Configuration>(jr);
                }
            }
            catch (Exception e)
            {
                
            }

            if (null == config)
                config = new Configuration();

            return config;
        }

        public bool WriteConfiguration()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CONFIG_FILENAME));

                JsonSerializer serializer = new JsonSerializer();
                using (StreamWriter sr = new StreamWriter(CONFIG_FILENAME))
                using (JsonWriter jr = new JsonTextWriter(sr))
                {
                    serializer.Serialize(jr, this);
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
    }
}