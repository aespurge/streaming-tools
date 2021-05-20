namespace streaming_tools {
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    /// <summary>
    ///     The persisted user configuration.
    /// </summary>
    public class Configuration : INotifyPropertyChanged {
        /// <summary>
        ///     The location the file should be saved and read from.
        /// </summary>
        private static readonly string CONFIG_FILENAME = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? string.Empty, "nullinside", "streaming-tools", "config.json");

        /// <summary>
        ///     The singleton instance of the class.
        /// </summary>
        private static Configuration? instance;

        /// <summary>
        /// The <seealso cref="Guid"/> of the microphone used to pause TTS.
        /// </summary>
        private string? microphoneGuid;

        /// <summary>
        /// The 0% - 100% microphone volume at which to pause TTS.
        /// </summary>
        private int pauseThreshold;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        /// <remarks>Prevents the class from being instantiated outside of our singleton.</remarks>
        protected Configuration() { }

        /// <summary>
        /// Raised when a property is changed on this object.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        ///     Gets or sets the <seealso cref="Guid"/> of the microphone used to pause TTS.
        /// </summary>
        public string? MicrophoneGuid {
            get => this.microphoneGuid;
            set {
                if (value == this.microphoneGuid)
                    return;

                this.microphoneGuid = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the percentage of microphone audio at which point text to speech pauses.
        /// </summary>
        public int PauseThreshold {
            get => this.pauseThreshold;
            set {
                if (value == this.pauseThreshold)
                    return;

                this.pauseThreshold = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the collection of all twitch accounts.
        /// </summary>
        public ObservableCollection<TwitchAccount>? TwitchAccounts { get; set; }

        /// <summary>
        /// Gets or sets the collection of twitch chat configurations.
        /// </summary>
        public ObservableCollection<TwitchChatConfiguration>? TwitchChatConfigs { get; set; }

        /// <summary>
        ///     Gets the singleton instance of our class.
        /// </summary>
        public static Configuration Instance {
            get {
                if (null == instance)
                    instance = ReadConfiguration();

                return instance;
            }
        }

        /// <summary>
        ///     Read the configuration from disk.
        /// </summary>
        /// <returns>The configuration object.</returns>
        public static Configuration ReadConfiguration() {
            Configuration? config = null;

            try {
                if (File.Exists(CONFIG_FILENAME)) {
                    JsonSerializer serializer = new();
                    using (StreamReader sr = new(CONFIG_FILENAME))
                    using (JsonReader jr = new JsonTextReader(sr)) {
                        config = serializer.Deserialize<Configuration>(jr);
                    }
                }
            } catch (Exception) { }

            if (null == config)
                config = new Configuration();

            if (null == config.TwitchAccounts)
                config.TwitchAccounts = new ObservableCollection<TwitchAccount>();

            if (null == config.TwitchChatConfigs)
                config.TwitchChatConfigs = new ObservableCollection<TwitchChatConfiguration>();

            return config;
        }

        /// <summary>
        /// Gets the twitch account object associated with the username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>The <see cref="TwitchAccount"/> object if found, null otherwise.</returns>
        public TwitchAccount? GetTwitchAccount(string? username) {
            if (null == this.TwitchAccounts || string.IsNullOrWhiteSpace(username))
                return null;

            return this.TwitchAccounts.FirstOrDefault(a => null != a.Username && a.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        ///     Write the configuration to disk.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool WriteConfiguration() {
            try {
                var dirName = Path.GetDirectoryName(CONFIG_FILENAME);
                if (null != dirName && !Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                JsonSerializer serializer = new();
                using (StreamWriter sr = new(CONFIG_FILENAME))
                using (JsonWriter jr = new JsonTextWriter(sr)) {
                    serializer.Serialize(jr, this);
                }
            } catch (Exception) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Raised when a property changes on this class.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    ///     Represents a twitch account.
    /// </summary>
    public class TwitchAccount {
        /// <summary>
        ///     Gets or sets the twitch OAuth token.
        /// </summary>
        public string? OAuth { get; set; }

        /// <summary>
        ///     Gets or sets the twitch username.
        /// </summary>
        public string? Username { get; set; }
    }

    /// <summary>
    /// Represents a single connection to a twitch chat by a single user.
    /// </summary>
    public class TwitchChatConfiguration {
        /// <summary>
        /// Gets or sets the twitch username.
        /// </summary>
        public string? AccountUsername { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the bot should provide administration commands.
        /// </summary>
        public bool AdminOn { get; set; }

        /// <summary>
        ///     Gets or sets the output device to send audio to.
        /// </summary>
        public string? OutputDevice { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether text to speech should pause when someone talks into the microphone.
        /// </summary>
        public bool PauseDuringSpeech { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether text to speech is on.
        /// </summary>
        public bool TtsOn { get; set; }

        /// <summary>
        ///     Gets or sets the selected Microsoft Text to Speech voice.
        /// </summary>
        public string? TtsVoice { get; set; }

        /// <summary>
        ///     Gets or sets the volume of the text to speech voice.
        /// </summary>
        public uint TtsVolume { get; set; }

        /// <summary>
        ///     Gets or sets the twitch channel to read chat from.
        /// </summary>
        public string? TwitchChannel { get; set; }
    }
}