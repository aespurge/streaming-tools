using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;
using Avalonia;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;

namespace notification_app.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        private BufferedWaveProvider bufferedWaveProvider;
        private readonly Configuration config;
        private bool pauseDuringSpeech;
        private int pauseThreshold;
        private MeteringSampleProvider postVolumeMeter;
        private int selectedInputDevice;
        private Thickness thresholdVisualizationMargin;
        private TwitchChatTTS tts;
        private bool ttsOn;
        private string ttsVoice;
        private uint ttsVolume;
        private string twitchChannel;
        private string twitchOauth;
        private string twitchUsername;
        private readonly Timer unpauseTimer;
        private int voiceVolume;
        private WaveInEvent waveInEvent;
        private WaveOutEvent waveOutEvent;

        public MainWindowViewModel() {
            unpauseTimer = new Timer(1000);
            unpauseTimer.Elapsed += UnpauseTimer_Elapsed;
            unpauseTimer.AutoReset = false;
            config = Configuration.Instance();
            TwitchUsername = config.TwitchUsername;
            TwitchChannel = config.TwitchChannel;
            TwitchOauth = Encoding.UTF8.GetString(Convert.FromBase64String(config.TwitchOauth));
            TtsVoice = config.TtsVoice;
            TtsVolume = config.TtsVolume;
            SelectedInputDevice = GetSelectMicrophoneDeviceIndex(config.MicrophoneGuid);
            PauseDuringSpeech = config.PauseDuringSpeech;
            PauseThreshold = config.PauseThreshold;

            PropertyChanged += MainWindowViewModel_PropertyChanged;

            TTSOn = config.TTSOn;
        }

        public string TwitchUsername {
            get => twitchUsername;
            set => this.RaiseAndSetIfChanged(ref twitchUsername, value);
        }

        public string TwitchOauth {
            get => twitchOauth;
            set => this.RaiseAndSetIfChanged(ref twitchOauth, value);
        }

        public string TwitchChannel {
            get => twitchChannel;
            set => this.RaiseAndSetIfChanged(ref twitchChannel, value);
        }

        public string TtsVoice {
            get => ttsVoice;
            set => this.RaiseAndSetIfChanged(ref ttsVoice, value);
        }

        public uint TtsVolume {
            get => ttsVolume;
            set => this.RaiseAndSetIfChanged(ref ttsVolume, value);
        }

        public bool PauseDuringSpeech {
            get => pauseDuringSpeech;
            set {
                if (value == pauseDuringSpeech) return;

                this.RaiseAndSetIfChanged(ref pauseDuringSpeech, value);

                if (value) {
                    waveInEvent = new WaveInEvent {DeviceNumber = selectedInputDevice - 1};
                    waveInEvent.DataAvailable += WaveInEvent_DataAvailable;
                    bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(8000, 1));

                    var sampleChannel = new SampleChannel(bufferedWaveProvider, true);
                    sampleChannel.PreVolumeMeter += SampleChannel_PreVolumeMeter;
                    sampleChannel.Volume = 100;
                    postVolumeMeter = new MeteringSampleProvider(sampleChannel);
                    // postVolumeMeter.StreamVolume += PostVolumeMeter_StreamVolume;

                    waveInEvent.StartRecording();
                } else {
                    waveInEvent.Dispose();
                    waveInEvent = null;

                    bufferedWaveProvider.ClearBuffer();
                    bufferedWaveProvider = null;
                    postVolumeMeter = null;
                }
            }
        }

        public int SelectedInputDevice {
            get => selectedInputDevice;
            set => this.RaiseAndSetIfChanged(ref selectedInputDevice, value);
        }

        public int VoiceVolume {
            get => voiceVolume;
            set => this.RaiseAndSetIfChanged(ref voiceVolume, value);
        }

        public int PauseThreshold {
            get => pauseThreshold;
            set {
                if (value < 0)
                    value = 0;
                else if (value > 100) value = 100;

                this.RaiseAndSetIfChanged(ref pauseThreshold, value);
                ThresholdVisualizationMargin = new Thickness(value, ThresholdVisualizationMargin.Top,
                    ThresholdVisualizationMargin.Right, ThresholdVisualizationMargin.Bottom);
            }
        }

        public Thickness ThresholdVisualizationMargin {
            get => thresholdVisualizationMargin;
            set => this.RaiseAndSetIfChanged(ref thresholdVisualizationMargin, value);
        }

        public bool TTSOn {
            get => ttsOn;
            set {
                if (value == ttsOn) return;

                this.RaiseAndSetIfChanged(ref ttsOn, value);

                if (value) {
                    config.TwitchUsername = TwitchUsername;
                    config.TwitchChannel = TwitchChannel;
                    config.TwitchOauth = Convert.ToBase64String(Encoding.UTF8.GetBytes(TwitchOauth));
                    config.TtsVoice = TtsVoice;
                    config.TtsVolume = TtsVolume;
                    config.MicrophoneGuid = GetSelectMicrophoneDeviceGuid();
                    config.PauseDuringSpeech = PauseDuringSpeech;
                    config.PauseThreshold = PauseThreshold;
                    config.TTSOn = value;
                    Configuration.Instance().WriteConfiguration();

                    tts = new TwitchChatTTS();
                    tts.Connect();
                } else {
                    tts.Dispose();

                    config.TTSOn = false;
                    Configuration.Instance().WriteConfiguration();
                }
            }
        }

        private void UnpauseTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (null != tts)
                tts.Unpause();
        }

        private string GetSelectMicrophoneDeviceGuid() {
            if (SelectedInputDevice < 0) return null;

            var totalDevices = NAudioUtilities.GetNumberOfDevices();
            var guids = Enumerable.Range(-1, totalDevices + 1)
                                  .Select(n => NAudioUtilities.GetCapabilities(n).ProductGuid).ToArray();
            return guids[SelectedInputDevice].ToString();
        }

        private int GetSelectMicrophoneDeviceIndex(string guid) {
            var totalDevices = NAudioUtilities.GetNumberOfDevices();
            var index = -1;
            for (var i = -1; i < totalDevices - 1; i++)
                if (NAudioUtilities.GetCapabilities(i).ProductGuid.ToString() == guid) {
                    index = i + 1;
                    break;
                }

            return index;
        }

        private void SampleChannel_PreVolumeMeter(object? sender, StreamVolumeEventArgs e) {
            VoiceVolume = Convert.ToInt32(e.MaxSampleValues[0] * 100);

            if (VoiceVolume > PauseThreshold) {
                if (null != tts)
                    tts.Pause();

                if (null != unpauseTimer) {
                    unpauseTimer.Stop();
                    unpauseTimer.Start();
                }
            }
        }

        private void WaveInEvent_DataAvailable(object? sender, WaveInEventArgs e) {
            bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            float[] test = new float[e.Buffer.Length];
            postVolumeMeter.Read(test, 0, e.BytesRecorded);
        }

        ~MainWindowViewModel() {
            PropertyChanged -= MainWindowViewModel_PropertyChanged;
        }

        private void MainWindowViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (!nameof(TTSOn).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase) &&
                !nameof(voiceVolume).Equals(e.PropertyName, StringComparison.InvariantCultureIgnoreCase))
                TTSOn = false;
        }
    }
}