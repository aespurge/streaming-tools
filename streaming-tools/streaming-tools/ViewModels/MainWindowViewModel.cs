namespace streaming_tools.ViewModels {
    using streaming_tools.GameIntegrations;

    /// <summary>
    ///     The business logic behind the main UI.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase {
        /// <summary>
        ///     A flag indicating whether the path of exile integration is turned on.
        /// </summary>
        private bool pathOfExileEnabled;

        /// <summary>
        ///     The path of exile game integration.
        /// </summary>
        private PathOfExileIntegration? poe;

        /// <summary>
        ///     The view responsible for specifying twitch accounts.
        /// </summary>
        public AccountsViewModel AccountsViewModel => new();

        /// <summary>
        ///     The view responsible for managing the sounds played for channel point redemptions.
        /// </summary>
        public ChannelPointViewModel ChannelPointViewModel => new();

        /// <summary>
        ///     The view responsible for managing the keystroke command.
        /// </summary>
        public KeystrokeCommandViewModel KeystrokeCommandViewModel => new();

        /// <summary>
        ///     The view responsible for laying out windows on the OS.
        /// </summary>
        public LayoutsViewModel LayoutViewModel => new();

        /// <summary>
        ///     Gets or sets a value indicating whether the path of exile integration is enable.
        /// </summary>
        public bool PathOfExileEnabled {
            get => this.pathOfExileEnabled;
            set {
                this.pathOfExileEnabled = value;

                if (value) {
                    this.poe = new PathOfExileIntegration();
                } else {
                    this.poe?.Dispose();
                    this.poe = null;
                }
            }
        }

        /// <summary>
        ///     The view responsible for pausing TTS when the microphone hears things.
        /// </summary>
        public TtsPauseConfigViewModel TtsPauseConfigViewModel => new();

        /// <summary>
        ///     The view model for the phonetic words list.
        /// </summary>
        public TtsPhoneticWordsViewModel TtsPhoneticWordsViewModel => new();

        /// <summary>
        ///     The view responsible for managing the list of usernames to skip.
        /// </summary>
        public TtsSkipUsernamesViewModel TtsSkipUsernamesViewModel => new();

        /// <summary>
        ///     The view responsible for holding the configurations for each twitch chat connection.
        /// </summary>
        public TwitchChatConfigsViewModel TwitchChatConfigs => new();
    }
}