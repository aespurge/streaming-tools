﻿using System;
using System.IO;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using NAudio.Wave;
using notification_app.NAudio;
using notification_app.Twitch.AdministrationFilter;
using notification_app.Twitch.TtsFilter;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace notification_app.Twitch {
    /// <summary>
    ///     A twitch chat Text-to-speech client.
    /// </summary>
    internal class TwitchChatTts : IDisposable {
        /// <summary>
        ///     Filters that administrate the chat.
        /// </summary>
        private readonly IAdminFilter[] adminFilters = {
            new BotWannaBecomeFamous()
        };

        /// <summary>
        ///     The persisted configuration.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     Filters for modifying an incoming message for text to speech.
        /// </summary>
        private readonly ITtsFilter[] ttsFilters = {
            new LinkFilter(),
            new UsernameFilter()
        };

        /// <summary>
        ///     The lock for ensuring mutual exclusion on the <see cref="ttsSoundOutput" /> object.
        /// </summary>
        private readonly object ttsSoundOutputLock = new();

        /// <summary>
        ///     The lock for ensuring mutual exclusion on the <see cref="ttsSoundOutputSignal" /> object.
        /// </summary>
        private readonly object ttsSoundOutputSignalLock = new();

        /// <summary>
        ///     The client capable of interacting with twitch chat.
        /// </summary>
        private TwitchClient? client;

        /// <summary>
        ///     The text-to-speech sound output.
        /// </summary>
        private WaveOutEvent ttsSoundOutput;

        /// <summary>
        ///     The signal used to make sound output synchronous.
        /// </summary>
        private ManualResetEvent ttsSoundOutputSignal;

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
            lock (ttsSoundOutputLock) {
                ttsSoundOutputSignal?.Set();
                ttsSoundOutputSignal?.Dispose();
                ttsSoundOutputSignal = null;
            }

            lock (ttsSoundOutputSignalLock) {
                ttsSoundOutput?.Stop();
                ttsSoundOutput?.Dispose();
                ttsSoundOutput = null;
            }

            client?.Disconnect();
            client = null;
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
            lock (ttsSoundOutputLock) {
                ttsSoundOutput?.Pause();
            }
        }

        /// <summary>
        ///     Unpauses the text to speech.
        /// </summary>
        public void Unpause() {
            lock (ttsSoundOutputLock) {
                ttsSoundOutput?.Play();
            }
        }

        /// <summary>
        ///     Event called when a message in received in twitch chat.
        /// </summary>
        /// <param name="sender">The twitch client object.</param>
        /// <param name="e">The chat message information.</param>
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e) {
            // First apply any administration filters where we may need to ban people from 
            // chat, etc. If the administration filter tells us that we shouldn't process
            // the message further because it handled it, then don't.
            foreach (var filter in adminFilters)
                if (!filter.handle(client, e))
                    return;

            // Go through the TTS filters which modify the chat message and update it.
            string chatMessage = e.ChatMessage.Message;
            foreach (var filter in ttsFilters) chatMessage = filter.filter(e, chatMessage);

            // If we don't have a chat message then the message was completely filtered out and we have nothing
            // to do here.
            if (null == chatMessage || string.IsNullOrWhiteSpace(chatMessage.Trim()))
                return;

            // If the chat message starts with the !tts command, then TTS is supposed to read the message as if
            // they're say it. So we will handle the message as such.
            if (!chatMessage.Trim().StartsWith("!tts", StringComparison.InvariantCultureIgnoreCase))
                chatMessage = $"{e.ChatMessage.DisplayName} says {chatMessage}";
            else
                chatMessage = chatMessage.Replace("!tts", "");

            // Create a microsoft TTS object and a stream for outputting its audio file to.
            using (var synth = new SpeechSynthesizer())
            using (var stream = new MemoryStream()) {
                // Setup the microsoft TTS object according to the settings.
                synth.SetOutputToWaveStream(stream);
                synth.SelectVoice(config.TtsVoice);
                synth.Volume = (int) config.TtsVolume;
                synth.Speak(chatMessage);

                // Now that we filled the stream, seek to the beginning so we can play it.
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new WaveFileReader(stream);

                try {
                    // Make sure we lock the objects used on multiple threads and play the file.
                    lock (ttsSoundOutputLock)
                    lock (ttsSoundOutputSignalLock) {
                        ttsSoundOutput = new WaveOutEvent();
                        ttsSoundOutputSignal = new ManualResetEvent(false);

                        ttsSoundOutput.DeviceNumber = getOutputDeviceIndex(config.OutputDevice);
                        ttsSoundOutput.Volume = config.TtsVolume / 100.0f;

                        ttsSoundOutput.Init(reader);

                        // Play is async so we will make it synchronous here so we don't have to deal with
                        // queueing. We can improve this to remove the hack in the future.
                        ttsSoundOutput.PlaybackStopped += delegate {
                            lock (ttsSoundOutputSignalLock) {
                                ttsSoundOutputSignal?.Set();
                            }
                        };

                        // Play it.
                        ttsSoundOutput.Play();
                    }

                    // Wait for the play to finish, we will get signaled.
                    var signal = ttsSoundOutputSignal;
                    ttsSoundOutputSignal?.WaitOne();
                } finally {
                    // Finally dispose of everything safely in the lock.
                    lock (ttsSoundOutputLock)
                    lock (ttsSoundOutputSignalLock) {
                        ttsSoundOutput?.Dispose();
                        ttsSoundOutput = null;
                        ttsSoundOutputSignal?.Dispose();
                        ttsSoundOutputSignal = null;
                    }
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