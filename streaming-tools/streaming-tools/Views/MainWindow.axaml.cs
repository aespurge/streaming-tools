using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Win32;
using notification_app.NAudio;
using notification_app.Utilities;
using PInvoke;

namespace notification_app.Views {
    public class MainWindow : Window {
        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        /// <summary>
        ///     Initializes the controls values.
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
            var devices = NAudioUtilities.GetTotalInputDevices();
            var list = Enumerable.Range(-1, devices + 1).Select(n => NAudioUtilities.GetInputDevice(n).ProductName).ToArray();
            inputSources.Items = list;

            // Setup the list of output devices
            var outputSources = this.Find<ComboBox>("outputDeviceComboBox");
            var outputDevices = NAudioUtilities.GetTotalOutputDevices();
            var outputItems = Enumerable.Range(-1, outputDevices + 1).Select(n => NAudioUtilities.GetOutputDevice(n).ProductName).ToArray();
            outputSources.Items = outputItems;
        }
    }
}