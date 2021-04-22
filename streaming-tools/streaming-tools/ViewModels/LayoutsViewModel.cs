using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using notification_app.Utilities;
using PInvoke;

namespace notification_app.ViewModels {
    public class LayoutsViewModel : ViewModelBase {
        public string SelectedMonitor { get; set; }

        public LayoutsViewModel() {
            var primaryMonitor = MonitorUtilities.getPrimaryMonitor();
            SelectedMonitor = (MonitorUtilities.MONITOR_NOT_FOUND_DEVICE_NAME != primaryMonitor.DeviceName) ? primaryMonitor.DeviceName : MonitorUtilities.getMonitors().FirstOrDefault().DeviceName;
        }

        private const int padding = -15;

        private void OnExecuteClicked() {
            // Get the monitors
            var monitors = MonitorUtilities.getMonitors();
            var processes = Process.GetProcessesByName("vlc").ToList();
            if (0 == processes.Count)
                return;

            // Trim minimized applications
            var appsOnMonitor = new int[monitors.Count];
            for (int i = processes.Count - 1; i >= 0; i--) {
                if (User32.IsIconic(processes[i].MainWindowHandle))
                    processes.RemoveAt(i);
            }

            // Find which monitor the applications are on
            foreach (var process in processes) {
                var handle = process.MainWindowHandle;

                RECT windowPosition;
                User32.GetWindowRect(handle, out windowPosition);

                for (int i = 0; i < monitors.Count; i++) {
                    var mon = monitors[i];

                    if (windowPosition.top >= mon.Monitor.Top && windowPosition.left >= mon.Monitor.Left &&
                        windowPosition.top < mon.Monitor.Bottom && windowPosition.left < mon.Monitor.Right) {
                        ++appsOnMonitor[i];
                    }
                }
            }

            // Get monitor with the most apps on it. 
            int maxIndex = 0;
            int maxValue = 0;
            for (int i = 0; i < appsOnMonitor.Length; i++) {
                if (appsOnMonitor[i] > maxValue) {
                    maxValue = appsOnMonitor[i];
                    maxIndex = i;
                }
            }

            // Determine the size of the windows when tiled based on the total area of
            // the monitor
            var monitor = monitors[maxIndex];
            var monitorWidth = monitor.WorkArea.Right - monitor.WorkArea.Left;
            var monitorHeight = monitor.WorkArea.Bottom - monitor.WorkArea.Top;
            var width = (2 <= processes.Count) ? (int) Math.Ceiling(monitorWidth / 2.0f) : monitorWidth;
            width += (int) ((padding / 2.0) * -1.0);
            var height = monitorHeight;
            var rows = Math.Ceiling(processes.Count / 2.0f);
            height = (int) Math.Ceiling(height / rows);

            // Apply the layout
            for (int i = 0; i < processes.Count; i++) {
                var process = processes[i];
                int row = (int) Math.Floor(i / 2.0);
                int column = (i % 2 == 0) ? 0 : 1;
                var x = monitor.WorkArea.Left + (column * width) + ((column == 1) ? padding : 0);
                var y = monitor.WorkArea.Top + (row * height);

                if (!previousWindowSettings.ContainsKey(process.Id))
                    previousWindowSettings[process.Id] = (User32.SetWindowLongFlags) User32.GetWindowLong(process.MainWindowHandle, User32.WindowLongIndexFlags.GWL_STYLE);

                User32.SetWindowLong(process.MainWindowHandle, User32.WindowLongIndexFlags.GWL_STYLE, User32.SetWindowLongFlags.WS_VISIBLE);
                User32.SetWindowPos(process.MainWindowHandle, User32.SpecialWindowHandles.HWND_TOP, x, y, width, height, User32.SetWindowPosFlags.SWP_SHOWWINDOW);
            }
        }

        private Dictionary<int, User32.SetWindowLongFlags> previousWindowSettings = new();

        private void OnUndoClicked() {
            foreach (var process in Process.GetProcessesByName("vlc")) {
                User32.SetWindowLongFlags oldValue;
                if (!previousWindowSettings.TryGetValue(process.Id, out oldValue))
                    continue;

                User32.SetWindowLong(process.MainWindowHandle, User32.WindowLongIndexFlags.GWL_STYLE, oldValue);
            }
        }
    }
}