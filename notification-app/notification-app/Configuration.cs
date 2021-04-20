using System;
using System.IO;
using Newtonsoft.Json;

namespace notification_app {
    /// <summary>
    ///     The persisted user configuration.
    /// </summary>
    internal class Configuration {
        /// <summary>
        ///     The location the file should be saved and read from.
        /// </summary>
        private static readonly string CONFIG_FILENAME = Path.Combine(
            Environment.GetEnvironmentVariable("LocalAppData"),
            "nullinside", "notification-app", "config.json");

        /// <summary>
        ///     The singleton instance of the class.
        /// </summary>
        private static Configuration instance;

        /// <summary>
        ///     Prevents classes from instantiated.
        /// </summary>
        protected Configuration() { }

        /// <summary>
        ///     The twitch user that connects to chats.
        /// </summary>
        public string TwitchUsername { get; set; }

        /// <summary>
        ///     The OAuth token used to authenticate as the <see cref="TwitchUsername" />.
        /// </summary>
        public string TwitchOauth { get; set; }

        /// <summary>
        ///     The twitch channel to read chat from.
        /// </summary>
        public string TwitchChannel { get; set; }

        /// <summary>
        ///     The selected Microsoft Text to Speech voice.
        /// </summary>
        public string TtsVoice { get; set; }

        /// <summary>
        ///     The output device to send audio to.
        /// </summary>
        public string OutputDevice { get; set; }

        /// <summary>
        ///     The volume of the text to speech voice.
        /// </summary>
        public uint TtsVolume { get; set; }

        /// <summary>
        ///     The GUID of the microphone to listen to.
        /// </summary>
        public string MicrophoneGuid { get; set; }

        /// <summary>
        ///     True if text to speech should pause when someone talks into the microphone, false otherwise.
        /// </summary>
        public bool PauseDuringSpeech { get; set; }

        /// <summary>
        ///     True if text to speech is on, false otherwise.
        /// </summary>
        public bool TTSOn { get; set; }

        /// <summary>
        ///     The percentage of microphone audio at which point text to speech pauses.
        /// </summary>
        public int PauseThreshold { get; set; }

        /// <summary>
        ///     The singleton instance of our class.
        /// </summary>
        /// <returns>The <see cref="Configuration" /> object.</returns>
        public static Configuration Instance() {
            if (null == instance) instance = ReadConfiguration();

            return instance;
        }

        /// <summary>
        ///     Read the configuration from disk.
        /// </summary>
        /// <returns>The configuration object.</returns>
        public static Configuration ReadConfiguration() {
            Configuration config = null;

            try {
                JsonSerializer serializer = new();
                using (StreamReader sr = new(CONFIG_FILENAME))
                using (JsonReader jr = new JsonTextReader(sr)) {
                    config = serializer.Deserialize<Configuration>(jr);
                }
            } catch (Exception e) { }

            if (null == config)
                config = new Configuration();

            return config;
        }

        /// <summary>
        ///     Write the configuration to disk.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool WriteConfiguration() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(CONFIG_FILENAME));

                JsonSerializer serializer = new();
                using (StreamWriter sr = new(CONFIG_FILENAME))
                using (JsonWriter jr = new JsonTextWriter(sr)) {
                    serializer.Serialize(jr, this);
                }
            } catch (Exception e) {
                return false;
            }

            return true;
        }
    }
}