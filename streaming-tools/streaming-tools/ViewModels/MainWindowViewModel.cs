using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using streaming_tools.GameIntegrations;
using streaming_tools.Twitch;
using streaming_tools.Utilities;

namespace streaming_tools.ViewModels {
    /// <summary>
    ///     The business logic behind the main UI.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase {
        /*********************
         * The main objects **
         *********************/
        /// <summary>
        ///     The persisted configuration.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     The timer used to unpause TTS at some point after microphone data is detected.
        /// </summary>
        private readonly Timer unpauseTimer;

        /// <summary>
        ///     The object that buffers tha data from the microphone.
        /// </summary>
        private BufferedWaveProvider microphoneBufferedData;

        /****************************************
         * The Microphone Pausing TTS Settings **
         ****************************************/
        /// <summary>
        ///     The main event that listens for microphone data.
        /// </summary>
        private WaveInEvent microphoneDataEvent;

        /// <summary>
        ///     The margin to use on the visual indicator for the pause threshold.
        /// </summary>
        private Thickness microphoneMicrophoneThresholdVisualizationMargin;

        /// <summary>
        ///     The current volume of the voice read from the microphone.
        /// </summary>
        private int microphoneMicrophoneVoiceVolume;

        /// <summary>
        ///     The object that converts the microphone data into a wave so we can read the volume.
        /// </summary>
        private MeteringSampleProvider microphoneVoiceData;

        /// <summary>
        ///     The string representation of the output device in windows.
        /// </summary>
        private string outputDevice;

        /*************************
         * The game integrations *
         *************************/
        /// <summary>
        ///     A flag indicating whether the path of exile integration is turned on.
        /// </summary>
        private bool pathOfExileEnabled;

        /// <summary>
        ///     True if we should pause TTS when the microphone is picking up sound, false otherwise.
        /// </summary>
        private bool pauseDuringSpeech;

        /// <summary>
        ///     The percentage of volume at which we should pause TTS when we detect microphone data.
        /// </summary>
        private int pauseThreshold;

        /// <summary>
        ///     The path of exile game integration.
        /// </summary>
        private PathOfExileIntegration poe;

        /// <summary>
        ///     The string representation of the microphone device name from NAudio.
        /// </summary>
        private int selectedMicrophone;

        /// <summary>
        ///     The TTS object responsible for managing and listening to text to speech.
        /// </summary>
        private TwitchChatTts tts;

        /************************************
         * TTS Main Configuration Settings **
         ************************************/
        /// <summary>
        ///     True if TTS is on, false otherwise.
        /// </summary>
        private bool ttsOn;

        /// <summary>
        ///     The string representation of the TTS voice from the Microsoft TTS library.
        /// </summary>
        private string ttsVoice;

        /// <summary>
        ///     The volume to play TTS at.
        /// </summary>
        private uint ttsVolume;

        /// <summary>
        ///     The twitch channel to listen to.
        /// </summary>
        private string twitchChannel;

        /// <summary>
        ///     The OAuth token of the account to listen to twitch chat on.
        /// </summary>
        private string twitchOauth;

        /// <summary>
        ///     The username of the user to join twitch chat as.
        /// </summary>
        private string twitchUsername;

        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        public MainWindowViewModel() {
            // Default to unpausing TTS 1 second after the microphone threshold has paused it.
            unpauseTimer = new Timer(1000);
            unpauseTimer.Elapsed += UnpauseTimer_Elapsed;
            unpauseTimer.AutoReset = false;

            // Get the configuration and assign the values.
            config = Configuration.Instance();
            TwitchUsername = config.TwitchUsername;
            TwitchChannel = config.TwitchChannel;
            if (null != config.TwitchOauth)
                TwitchOauth = Encoding.UTF8.GetString(Convert.FromBase64String(config.TwitchOauth));
            TtsVoice = config.TtsVoice;
            OutputDevice = config.OutputDevice;
            TtsVolume = config.TtsVolume;
            SelectedMicrophone = GetSelectMicrophoneDeviceIndex(config.MicrophoneGuid);
            PauseDuringSpeech = config.PauseDuringSpeech;
            PauseThreshold = config.PauseThreshold;

            // We listen to our own property changed event to know when we need to push
            // information to the TTS object.
            PropertyChanged += MainWindowViewModel_PropertyChanged;
            TtsOn = config.TtsOn;
        }

        /*********************************
         * The inner control view models *
         *********************************/
        /// <summary>
        ///     The view responsible for laying out windows on the OS.
        /// </summary>
        private LayoutsViewModel LayoutViewModel => new();

        /// <summary>
        ///     True if TTS is on, false otherwise.
        /// </summary>
        public bool TtsOn {
            get => ttsOn;
            set {
                if (value == ttsOn) return;

                this.RaiseAndSetIfChanged(ref ttsOn, value);

                if (Design.IsDesignMode)
                    return;

                if (value) {
                    tts = new TwitchChatTts();
                    tts.Connect();
                } else {
                    if (null != tts)
                        tts.Dispose();
                }
            }
        }

        /// <summary>
        ///     True if path of exile integration is enable, false otherwise.
        /// </summary>
        public bool PathOfExileEnabled {
            get => pathOfExileEnabled;
            set {
                pathOfExileEnabled = value;

                if (value) {
                    poe = new PathOfExileIntegration();
                } else {
                    poe?.Dispose();
                    poe = null;
                }
            }
        }

        /// <summary>
        ///     The username of the user to join twitch chat as.
        /// </summary>
        public string TwitchUsername {
            get => twitchUsername;
            set => this.RaiseAndSetIfChanged(ref twitchUsername, value);
        }

        /// <summary>
        ///     The OAuth token of the account to listen to twitch chat on.
        /// </summary>
        public string TwitchOauth {
            get => twitchOauth;
            set => this.RaiseAndSetIfChanged(ref twitchOauth, value);
        }

        /// <summary>
        ///     The twitch channel to listen to.
        /// </summary>
        public string TwitchChannel {
            get => twitchChannel;
            set => this.RaiseAndSetIfChanged(ref twitchChannel, value);
        }

        /// <summary>
        ///     The string representation of the TTS voice from the Microsoft TTS library.
        /// </summary>
        public string TtsVoice {
            get => ttsVoice;
            set => this.RaiseAndSetIfChanged(ref ttsVoice, value);
        }

        /// <summary>
        ///     The string representation of the output device in windows.
        /// </summary>
        public string OutputDevice {
            get => outputDevice;
            set => this.RaiseAndSetIfChanged(ref outputDevice, value);
        }

        /// <summary>
        ///     The volume to play TTS at.
        /// </summary>
        public uint TtsVolume {
            get => ttsVolume;
            set => this.RaiseAndSetIfChanged(ref ttsVolume, value);
        }

        /// <summary>
        ///     True if we should pause TTS when the microphone is picking up sound, false otherwise.
        /// </summary>
        public bool PauseDuringSpeech {
            get => pauseDuringSpeech;
            set {
                if (value == pauseDuringSpeech) return;

                this.RaiseAndSetIfChanged(ref pauseDuringSpeech, value);

                if (value)
                    StartListenToMicrophone();
                else
                    StopListenToMicrophone();
            }
        }

        /// <summary>
        ///     The string representation of the microphone device name from NAudio.
        /// </summary>
        public int SelectedMicrophone {
            get => selectedMicrophone;
            set => this.RaiseAndSetIfChanged(ref selectedMicrophone, value);
        }

        /// <summary>
        ///     The current volume of the voice read from the microphone.
        /// </summary>
        public int MicrophoneVoiceVolume {
            get => microphoneMicrophoneVoiceVolume;
            set => this.RaiseAndSetIfChanged(ref microphoneMicrophoneVoiceVolume, value);
        }

        /// <summary>
        ///     The percentage of volume at which we should pause TTS when we detect microphone data.
        /// </summary>
        public int PauseThreshold {
            get => pauseThreshold;
            set {
                if (value < 0)
                    value = 0;
                else if (value > 100) value = 100;

                this.RaiseAndSetIfChanged(ref pauseThreshold, value);
                MicrophoneThresholdVisualizationMargin = new Thickness(value, MicrophoneThresholdVisualizationMargin.Top,
                    MicrophoneThresholdVisualizationMargin.Right, MicrophoneThresholdVisualizationMargin.Bottom);
            }
        }

        /// <summary>
        ///     The margin used to push the visual representation of the threshold marker on the UI.
        /// </summary>
        public Thickness MicrophoneThresholdVisualizationMargin {
            get => microphoneMicrophoneThresholdVisualizationMargin;
            set => this.RaiseAndSetIfChanged(ref microphoneMicrophoneThresholdVisualizationMargin, value);
        }

        /// <summary>
        ///     Destructor
        /// </summary>
        ~MainWindowViewModel() {
            PropertyChanged -= MainWindowViewModel_PropertyChanged;
        }

        /// <summary>
        ///     Starts listening to the microphone so we know when to pause TTS for microphone speaking.
        /// </summary>
        private void StartListenToMicrophone() {
            if (-1 == SelectedMicrophone)
                return;

            microphoneDataEvent = new WaveInEvent {DeviceNumber = SelectedMicrophone - 1};
            microphoneDataEvent.DataAvailable += Microphone_DataReceived;
            microphoneBufferedData = new BufferedWaveProvider(new WaveFormat(8000, 1));

            var sampleChannel = new SampleChannel(microphoneBufferedData, true);
            sampleChannel.PreVolumeMeter += MicrophoneAudioChannel_PreVolumeMeter;
            sampleChannel.Volume = 100;
            microphoneVoiceData = new MeteringSampleProvider(sampleChannel);
            // microphoneVoiceData.StreamVolume += PostVolumeMeter_StreamVolume;

            microphoneDataEvent.StartRecording();
        }

        /// <summary>
        ///     Stops listening to the microphone.
        /// </summary>
        private void StopListenToMicrophone() {
            microphoneDataEvent?.Dispose();
            microphoneDataEvent = null;

            microphoneBufferedData?.ClearBuffer();
            microphoneBufferedData = null;
            microphoneVoiceData = null;
        }

        /// <summary>
        ///     Event fired after TTS has been paused to unpause it. This occurs after the TTS
        ///     has been paused by the voice detected in the microphone has exceeded the threshold.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private void UnpauseTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (null != tts)
                tts.Unpause();
        }

        /// <summary>
        ///     Gets the selected microphone unique GUID.
        /// </summary>
        /// <returns></returns>
        private string GetSelectMicrophoneDeviceGuid() {
            if (SelectedMicrophone < 0) return null;

            var totalDevices = NAudioUtilities.GetTotalInputDevices();
            var guids = Enumerable.Range(-1, totalDevices + 1)
                                  .Select(n => NAudioUtilities.GetInputDevice(n).ProductGuid).ToArray();
            return guids[SelectedMicrophone].ToString();
        }

        /// <summary>
        ///     Gets the NAudio index associated with the selected microphone.
        /// </summary>
        /// <param name="guid">The unique GUID of the microphone.</param>
        /// <returns>The index of the microphone according to NAudio.</returns>
        private int GetSelectMicrophoneDeviceIndex(string guid) {
            var totalDevices = NAudioUtilities.GetTotalInputDevices();
            var index = -1;
            for (var i = -1; i < totalDevices - 1; i++)
                if (NAudioUtilities.GetInputDevice(i).ProductGuid.ToString() == guid) {
                    index = i + 1;
                    break;
                }

            return index;
        }

        /// <summary>
        ///     Called when microphone voice data is recognized.
        /// </summary>
        /// <param name="sender">A <seealso cref="SampleChannel" /> receiving microphone data.</param>
        /// <param name="e">The data on how loud the voice is.</param>
        private void MicrophoneAudioChannel_PreVolumeMeter(object? sender, StreamVolumeEventArgs e) {
            MicrophoneVoiceVolume = Convert.ToInt32(e.MaxSampleValues[0] * 100);

            if (MicrophoneVoiceVolume > PauseThreshold) {
                if (null != tts)
                    tts.Pause();

                if (null != unpauseTimer) {
                    unpauseTimer.Stop();
                    unpauseTimer.Start();
                }
            }
        }

        /// <summary>
        ///     Captures all property values that change on the class.
        /// </summary>
        /// <param name="sender">The window.</param>
        /// <param name="e">Information on the property that changed.</param>
        private void MainWindowViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (nameof(SelectedMicrophone).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase)) {
                StopListenToMicrophone();
                StartListenToMicrophone();
            }

            WriteConfiguration();
        }

        /// <summary>
        ///     Write the configuration to file.
        /// </summary>
        private void WriteConfiguration() {
            config.TwitchUsername = TwitchUsername;
            config.TwitchChannel = TwitchChannel;
            if (null != TwitchOauth)
                config.TwitchOauth = Convert.ToBase64String(Encoding.UTF8.GetBytes(TwitchOauth));
            config.TtsVoice = TtsVoice;
            config.OutputDevice = OutputDevice;
            config.TtsVolume = TtsVolume;
            config.MicrophoneGuid = GetSelectMicrophoneDeviceGuid();
            config.PauseDuringSpeech = PauseDuringSpeech;
            config.PauseThreshold = PauseThreshold;
            config.TtsOn = TtsOn;
            Configuration.Instance().WriteConfiguration();
        }

        /// <summary>
        ///     Called when the microphone creates voice data.
        /// </summary>
        /// <param name="sender">The <seealso cref="WaveInEvent" /> that captured the data from the microphone.</param>
        /// <param name="e">Data received.</param>
        private void Microphone_DataReceived(object? sender, WaveInEventArgs e) {
            microphoneBufferedData.AddSamples(e.Buffer, 0, e.BytesRecorded);
            float[] test = new float[e.Buffer.Length];
            microphoneVoiceData.Read(test, 0, e.BytesRecorded);
        }
    }
}