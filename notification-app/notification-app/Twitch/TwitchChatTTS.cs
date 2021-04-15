using System;
using System.Speech.Synthesis;
using System.Text;
using notification_app.Twitch.Filter;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace notification_app {
    /// <summary>
    /// A twitch chat Text-to-speech client.
    /// </summary>
    internal class TwitchChatTts : IDisposable {
        /// <summary>
        /// The persisted configuration.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        /// The client capable of interacting with twitch chat.
        /// </summary>
        private TwitchClient? client;

        /// <summary>
        /// The text-to-speech engine.
        /// </summary>
        private SpeechSynthesizer synth = new();

        private ITtsFilter[] filters = new ITtsFilter[] {
            new LinkFilter(),
            new UsernameFilter()
        };

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public TwitchChatTts() {
            config = Configuration.Instance();

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();
            synth.Volume = (int) config.TtsVolume;

            try {
                // Pick the voice
                synth.SelectVoice(config.TtsVoice);
            } catch (Exception e) { }
        }

        /// <summary>
        /// Releases unmanaged resources.
        /// </summary>
        public void Dispose() {
            client?.Disconnect();
            client = null;
            synth?.Dispose();
            synth = null;
        }

        /// <summary>
        /// Connects to the chat to listen for messages to read in text to speech.
        /// </summary>
        public void Connect() {
            byte[] data = Convert.FromBase64String(config.TwitchOauth);
            string password = Encoding.UTF8.GetString(data);
            ConnectionCredentials credentials = new(config.TwitchUsername, password);
            var clientOptions = new ClientOptions {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, config.TwitchChannel);

            client.OnMessageReceived += Client_OnMessageReceived;
            client.Connect();
        }

        /// <summary>
        /// Pauses the text to speech.
        /// </summary>
        public void Pause() {
            synth?.Pause();
        }

        /// <summary>
        /// Unpauses the text to speech.
        /// </summary>
        public void Unpause() {
            synth?.Resume();
        }

        /// <summary>
        /// Event called when a message in received in twitch chat.
        /// </summary>
        /// <param name="sender">The twitch client object.</param>
        /// <param name="e">The chat message information.</param>
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e) {
            string chatMessage = e.ChatMessage.Message;
            foreach (var filter in filters) {
                chatMessage = filter.filter(e, chatMessage);
            }

            if (null == chatMessage || String.IsNullOrWhiteSpace(chatMessage.Trim()))
                return;

            if (!chatMessage.Trim().StartsWith("!tts", StringComparison.InvariantCultureIgnoreCase)) {
                synth?.SpeakAsync($"{e.ChatMessage.DisplayName} says {chatMessage}");
            }
            else {
                chatMessage = chatMessage.Replace("!tts", "");
                synth?.SpeakAsync($"{chatMessage}");
            }
        }
    }
}