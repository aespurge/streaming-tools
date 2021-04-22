using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PInvoke;

namespace notification_app.Utilities {
    static class MonitorUtilities {
        public const string MONITOR_NOT_FOUND_DEVICE_NAME = "MONITOR_NOT_FOUND";
        public static readonly PInvokeUtilities.MonitorInfoEx MONITOR_NOT_FOUND = new() {DeviceName = MONITOR_NOT_FOUND_DEVICE_NAME};

        public static List<PInvokeUtilities.MonitorInfoEx> getMonitors() {
            var monitors = new List<PInvokeUtilities.MonitorInfoEx>();

            unsafe {
                User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) => {
                    PInvokeUtilities.MonitorInfoEx mi = new PInvokeUtilities.MonitorInfoEx();
                    mi.Size = Marshal.SizeOf(mi);
                    bool success = PInvokeUtilities.GetMonitorInfo(monitor, ref mi);
                    if (success)
                        monitors.Add(mi);

                    return true;
                }, IntPtr.Zero);
            }

            return monitors;
        }

        public static PInvokeUtilities.MonitorInfoEx getPrimaryMonitor() {
            foreach (var monitor in getMonitors()) {
                if (((User32.MONITORINFO_Flags) monitor.Flags).HasFlag(User32.MONITORINFO_Flags.MONITORINFOF_PRIMARY))
                    return monitor;
            }

            return MONITOR_NOT_FOUND;
        }
    }
}