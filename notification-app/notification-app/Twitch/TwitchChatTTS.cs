using System;
using System.Speech.Synthesis;
using System.Text;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace notification_app {
    internal class TwitchChatTTS : IDisposable {
        private TwitchClient client;
        private readonly Configuration config;
        private SpeechSynthesizer synth = new();

        public TwitchChatTTS() {
            config = Configuration.Instance();

            // Configure the audio output.   
            synth.SetOutputToDefaultAudioDevice();
            synth.Volume = (int) config.TtsVolume;

            try {
                // Pick the voice
                if (null != config.TtsVoice)
                    synth.SelectVoice(config.TtsVoice);
            } catch (Exception e) { }
        }

        public void Dispose() {
            client.Disconnect();
            client = null;
            synth.Dispose();
            synth = null;
        }

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

        public void Pause() {
            if (null != synth)
                synth.Pause();
        }

        public void Unpause() {
            synth.Resume();
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e) {
            synth.SpeakAsync($"{e.ChatMessage.DisplayName} says {e.ChatMessage.Message}");
        }
    }
}