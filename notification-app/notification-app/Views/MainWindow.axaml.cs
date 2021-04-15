using System.Linq;
using System.Speech.Synthesis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace notification_app.Views {
    public class MainWindow : Window {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        /// <summary>
        /// Initializes the controls values.
        /// </summary>
        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            // Setup the list of voices
            var ttsVoices = this.Find<ComboBox>("ttsVoiceComboBox");
            var speech = new SpeechSynthesizer();
            ttsVoices.Items = speech.GetInstalledVoices().Select(v => v.VoiceInfo.Name);
            speech.Dispose();

            // Setup the list of microphone sources
            var inputSources = this.Find<ComboBox>("micSources");

            var devices = NAudioUtilities.GetNumberOfDevices();
            var list = Enumerable.Range(-1, devices + 1).Select(n => NAudioUtilities.GetWaveInDevice(n).ProductName).ToArray();
            inputSources.Items = list;
        }
    }
}