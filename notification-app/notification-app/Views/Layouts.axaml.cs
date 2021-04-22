using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using notification_app.Utilities;
using PInvoke;

namespace notification_app.Views {
    public class Layouts : UserControl {
        public Layouts() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            // Setup the list of monitors
            var monitors = this.Find<ComboBox>("monitors");
            var monitorsFound = MonitorUtilities.getMonitors();
            var monitorItems = Enumerable.Range(0, monitorsFound.Count).Select(n => monitorsFound[n].DeviceName).ToArray();
            monitors.Items = monitorItems;
        }
    }
}