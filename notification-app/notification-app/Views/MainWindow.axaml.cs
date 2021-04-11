using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Speech.Synthesis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NAudio;
using NAudio.Wave;

namespace notification_app.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Setup the list of voices
            var ttsVoices = this.Find<ComboBox>("ttsVoiceComboBox");
            var speech = new SpeechSynthesizer();
            ttsVoices.Items = speech.GetInstalledVoices().Select(v => v.VoiceInfo.Name);
            speech.Dispose();

            // Setup the list of microphone sources
            var inputSources = this.Find<ComboBox>("micSources");
            
            var devices = NAudioUtilities.GetNumberOfDevices();
            var list = Enumerable.Range(-1, devices + 1).Select(n => NAudioUtilities.GetCapabilities(n).ProductName).ToArray();
            inputSources.Items = list;
        }
    }
}
