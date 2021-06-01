namespace streaming_tools.Twitch.Tts {
    using System;
    using System.IO;
    using System.Speech.Synthesis;
    using System.Threading;

    using NAudio.Wave;

    using streaming_tools.Twitch.Tts.TtsFilter;
    using streaming_tools.Utilities;

    using TwitchLib.Client;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     A twitch chat Text-to-speech client.
    /// </summary>
    public class TwitchChatTts : IDisposable {
        /// <summary>
        ///     The configuration for the twitch chat.
        /// </summary>
        private readonly TwitchChatConfiguration? chatConfig;

        /// <summary>
        ///     Filters for modifying an incoming message for text to speech.
        /// </summary>
        private readonly ITtsFilter[] ttsFilters = { new LinkFilter(), new UsernameSkipFilter(), new UsernameRemoveCharactersFilter(), new UsernamePhoneticFilter(), new CommandFilter() };

        /// <summary>
        ///     The lock for ensuring mutual exclusion on the <see cref="ttsSoundOutput" /> object.
        /// </summary>
        private readonly object ttsSoundOutputLock = new();

        /// <summary>
        ///     The lock for ensuring mutual exclusion on the <see cref="ttsSoundOutputSignal" /> object.
        /// </summary>
        private readonly object ttsSoundOutputSignalLock = new();

        /// <summary>
        ///     The text-to-speech sound output.
        /// </summary>
        private WaveOutEvent? ttsSoundOutput;

        /// <summary>
        ///     The signal used to make sound output synchronous.
        /// </summary>
        private ManualResetEvent? ttsSoundOutputSignal;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatTts" /> class.
        /// </summary>
        /// <param name="config">The configuration for the twitch chat.</param>
        public TwitchChatTts(TwitchChatConfiguration? config) {
            this.chatConfig = config;
            GlobalKeyboardListener.Instance.Callback += this.KeyboardPressCallback;
        }

        /// <summary>
        ///     Connects to the chat to listen for messages to read in text to speech.
        /// </summary>
        public void Connect() {
            if (null == this.chatConfig)
                return;

            var config = Configuration.Instance;
            var user = config.GetTwitchAccount(this.chatConfig.AccountUsername);
            if (null == user)
                return;

            var twitchManager = TwitchChatManager.Instance;
            twitchManager.AddTwitchChannel(user, this.chatConfig.TwitchChannel, this.Client_OnMessageReceived);
        }

        /// <summary>
        ///     Releases unmanaged resources.
        /// </summary>
        public void Dispose() {
            lock (this.ttsSoundOutputLock) {
                this.ttsSoundOutputSignal?.Set();
                this.ttsSoundOutputSignal?.Dispose();
                this.ttsSoundOutputSignal = null;
            }

            lock (this.ttsSoundOutputSignalLock) {
                this.ttsSoundOutput?.Stop();
                this.ttsSoundOutput?.Dispose();
                this.ttsSoundOutput = null;
            }

            if (null == this.chatConfig)
                return;

            var user = Configuration.Instance.GetTwitchAccount(this.chatConfig.AccountUsername);
            if (null == user)
                return;

            var twitchManager = TwitchChatManager.Instance;
            twitchManager.RemoveTwitchChannel(user, this.chatConfig.TwitchChannel, this.Client_OnMessageReceived);
        }

        /// <summary>
        ///     Pauses the text to speech.
        /// </summary>
        public void Pause() {
            lock (this.ttsSoundOutputLock) {
                this.ttsSoundOutput?.Pause();
            }
        }

        /// <summary>
        ///     Continues the text to speech.
        /// </summary>
        public void Unpause() {
            lock (this.ttsSoundOutputLock) {
                this.ttsSoundOutput?.Play();
            }
        }

        /// <summary>
        ///     Event called when a message in received in twitch chat.
        /// </summary>
        /// <param name="twitchClient">The twitch chat client.</param>
        /// <param name="e">The chat message information.</param>
        private void Client_OnMessageReceived(TwitchClient twitchClient, OnMessageReceivedArgs e) {
            if (null == this.chatConfig)
                return;

            // Go through the TTS filters which modify the chat message and update it.
            var chatMessageInfo = new Tuple<string, string>(e.ChatMessage.DisplayName, e.ChatMessage.Message);
            foreach (var filter in this.ttsFilters) {
                if (null == chatMessageInfo)
                    break;

                chatMessageInfo = filter.Filter(e, chatMessageInfo.Item1, chatMessageInfo.Item2);
            }

            // If we don't have a chat message then the message was completely filtered out and we have nothing
            // to do here.
            if (null == chatMessageInfo || string.IsNullOrWhiteSpace(chatMessageInfo.Item2.Trim()))
                return;

            // If the chat message starts with the !tts command, then TTS is supposed to read the message as if
            // they're say it. So we will handle the message as such.
            string chatMessage;
            if (!chatMessageInfo.Item2.Trim().StartsWith("!tts", StringComparison.InvariantCultureIgnoreCase))
                chatMessage = $"{chatMessageInfo.Item1} says {chatMessageInfo.Item2}";
            else
                chatMessage = chatMessageInfo.Item2.Replace("!tts", "");

            // Create a microsoft TTS object and a stream for outputting its audio file to.
            using (var synth = new SpeechSynthesizer())
            using (var stream = new MemoryStream()) {
                // Setup the microsoft TTS object according to the settings.
                synth.SetOutputToWaveStream(stream);
                synth.SelectVoice(this.chatConfig.TtsVoice);
                synth.Volume = (int)this.chatConfig.TtsVolume;
                synth.Speak(chatMessage);

                // Now that we filled the stream, seek to the beginning so we can play it.
                stream.Seek(0, SeekOrigin.Begin);
                var reader = new WaveFileReader(stream);

                try {
                    // Make sure we lock the objects used on multiple threads and play the file.
                    lock (this.ttsSoundOutputLock)
                    lock (this.ttsSoundOutputSignalLock) {
                        this.ttsSoundOutput = new WaveOutEvent();
                        this.ttsSoundOutputSignal = new ManualResetEvent(false);

                        this.ttsSoundOutput.DeviceNumber = NAudioUtilities.GetOutputDeviceIndex(this.chatConfig.OutputDevice);
                        this.ttsSoundOutput.Volume = this.chatConfig.TtsVolume / 100.0f;

                        this.ttsSoundOutput.Init(reader);

                        // Play is async so we will make it synchronous here so we don't have to deal with
                        // queueing. We can improve this to remove the hack in the future.
                        this.ttsSoundOutput.PlaybackStopped += delegate {
                            lock (this.ttsSoundOutputSignalLock) {
                                this.ttsSoundOutputSignal?.Set();
                            }
                        };

                        // Play it.
                        this.ttsSoundOutput.Play();
                    }

                    // Wait for the play to finish, we will get signaled.
                    var signal = this.ttsSoundOutputSignal;
                    this.ttsSoundOutputSignal?.WaitOne();
                } finally {
                    // Finally dispose of everything safely in the lock.
                    lock (this.ttsSoundOutputLock)
                    lock (this.ttsSoundOutputSignalLock) {
                        this.ttsSoundOutput?.Dispose();
                        this.ttsSoundOutput = null;
                        this.ttsSoundOutputSignal?.Dispose();
                        this.ttsSoundOutputSignal = null;
                    }
                }
            }
        }

        /// <summary>
        ///     Handles key press anywhere in the OS.
        /// </summary>
        /// <param name="keyboard">The key that was pressed in string format.</param>
        /// <remarks>
        ///     See:
        ///     <see href="https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes?redirectedfrom=MSDN" />
        /// </remarks>
        private void KeyboardPressCallback(string keyboard) {
            if ("123".Equals(keyboard, StringComparison.InvariantCultureIgnoreCase))
                this.ttsSoundOutput?.Stop();
        }
    }
}