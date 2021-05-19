using System;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;

namespace streaming_tools {
    /// <summary>
    ///     The persisted user configuration.
    /// </summary>
    public class Configuration {
        /// <summary>
        ///     The location the file should be saved and read from.
        /// </summary>
        private static readonly string CONFIG_FILENAME = Path.Combine(
            Environment.GetEnvironmentVariable("LocalAppData"),
            "nullinside", "streaming-tools", "config.json");

        /// <summary>
        ///     The singleton instance of the class.
        /// </summary>
        private static Configuration instance;

        /// <summary>
        ///     Prevents classes from instantiated.
        /// </summary>
        protected Configuration() { }

        /// <summary>
        ///     A collection of all twitch accounts.
        /// </summary>
        public ObservableCollection<TwitchAccount> TwitchAccounts { get; set; }

        /// <summary>
        ///     The currently selected twitch account.
        /// </summary>
        public string SelectedTwitchAccount { get; set; }

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
        public bool TtsOn { get; set; }

        /// <summary>
        ///     The percentage of microphone audio at which point text to speech pauses.
        /// </summary>
        public int PauseThreshold { get; set; }

        /// <summary>
        ///     The singleton instance of our class.
        /// </summary>
        /// <returns>The <see cref="Configuration" /> object.</returns>
        public static Configuration Instance() {
            if (null == instance)
                instance = ReadConfiguration();

            return instance;
        }

        /// <summary>
        ///     Read the configuration from disk.
        /// </summary>
        /// <returns>The configuration object.</returns>
        public static Configuration ReadConfiguration() {
            Configuration config = null;

            try {
                if (File.Exists(CONFIG_FILENAME)) {
                    JsonSerializer serializer = new();
                    using (StreamReader sr = new(CONFIG_FILENAME))
                    using (JsonReader jr = new JsonTextReader(sr)) {
                        config = serializer.Deserialize<Configuration>(jr);
                    }
                }
            } catch (Exception e) { }

            if (null == config)
                config = new Configuration();

            if (null == config.TwitchAccounts)
                config.TwitchAccounts = new ObservableCollection<TwitchAccount>();

            return config;
        }

        /// <summary>
        ///     Write the configuration to disk.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool WriteConfiguration() {
            try {
                if (!Directory.Exists(Path.GetDirectoryName(CONFIG_FILENAME)))
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

    /// <summary>
    ///     Represents a twitch account.
    /// </summary>
    public class TwitchAccount {
        /// <summary>
        ///     The twitch username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     The twitch OAuth token.
        /// </summary>
        public string OAuth { get; set; }
    }
}