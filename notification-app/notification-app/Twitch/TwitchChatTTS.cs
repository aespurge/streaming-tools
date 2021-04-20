using System;
using System.IO;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using NAudio.Wave;
using notification_app.AdministrationFilter;
using notification_app.Twitch.AdministrationFilter;
using notification_app.Twitch.Filter;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace notification_app {
    /// <summary>
    ///     A twitch chat Text-to-speech client.
    /// </summary>
    internal class TwitchChatTts : IDisposable {
        /// <summary>
        ///     The persisted configuration.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     Filters that administrate the chat.
        /// </summary>
        private readonly IAdminFilter[] adminFilters = {
            new BotWannaBecomeFamous()
        };

        /// <summary>
        ///     The client capable of interacting with twitch chat.
        /// </summary>
        private TwitchClient? client;

        /// <summary>
        ///     The text-to-speech engine.
        /// </summary>
        private SpeechSynthesizer synth = new();

        /// <summary>
        ///     Filters for modifying an incoming message for text to speech.
        /// </summary>
        private readonly ITtsFilter[] ttsFilters = {
            new LinkFilter(),
            new UsernameFilter()
        };

        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        public TwitchChatTts() {
            config = Configuration.Instance();
        }

        /// <summary>
        ///     Releases unmanaged resources.
        /// </summary>
        public void Dispose() {
            client?.Disconnect();
            client = null;
            synth?.Dispose();
            synth = null;
        }

        /// <summary>
        ///     Connects to the chat to listen for messages to read in text to speech.
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
        ///     Pauses the text to speech.
        /// </summary>
        public void Pause() {
            synth?.Pause();
        }

        /// <summary>
        ///     Unpauses the text to speech.
        /// </summary>
        public void Unpause() {
            synth?.Resume();
        }

        /// <summary>
        ///     Event called when a message in received in twitch chat.
        /// </summary>
        /// <param name="sender">The twitch client object.</param>
        /// <param name="e">The chat message information.</param>
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e) {
            foreach (var filter in adminFilters)
                if (!filter.handle(client, e))
                    return;

            string chatMessage = e.ChatMessage.Message;
            foreach (var filter in ttsFilters) chatMessage = filter.filter(e, chatMessage);

            if (null == chatMessage || string.IsNullOrWhiteSpace(chatMessage.Trim()))
                return;

            if (!chatMessage.Trim().StartsWith("!tts", StringComparison.InvariantCultureIgnoreCase))
                chatMessage = $"{e.ChatMessage.DisplayName} says {chatMessage}";
            else
                chatMessage = chatMessage.Replace("!tts", "");

            using (var synth = new SpeechSynthesizer())
            using (var stream = new MemoryStream()) {
                synth.SetOutputToWaveStream(stream);
                synth.SelectVoice(config.TtsVoice);
                synth.Volume = (int) config.TtsVolume;
                synth.Speak(chatMessage);

                stream.Seek(0, SeekOrigin.Begin);
                var reader = new WaveFileReader(stream);
                using (var waveOut = new WaveOutEvent())
                using (var signal = new ManualResetEvent(false)) {
                    waveOut.DeviceNumber = getOutputDeviceIndex(config.OutputDevice);
                    waveOut.Volume = config.TtsVolume / 100.0f;

                    waveOut.Init(reader);
                    waveOut.Play();
                    waveOut.PlaybackStopped += delegate { signal.Set(); };

                    signal.WaitOne();
                }
            }
        }

        /// <summary>
        ///     Converts the device name to an index.
        /// </summary>
        /// <param name="name">The name of the device.</param>
        /// <returns>The index of the device if found, -1 otherwise.</returns>
        private int getOutputDeviceIndex(string name) {
            if (string.IsNullOrWhiteSpace(name)) return -1;

            for (var i = 0; i < NAudioUtilities.GetTotalOutputDevices(); i++) {
                var capability = NAudioUtilities.GetOutputDevice(i);

                if (name.Equals(capability.ProductName, StringComparison.InvariantCultureIgnoreCase)) return i;
            }

            return -1;
        }
    }
}