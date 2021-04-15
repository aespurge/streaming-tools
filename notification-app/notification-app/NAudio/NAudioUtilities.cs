using System;
using System.Runtime.InteropServices;
using NAudio;
using NAudio.Wave;

namespace notification_app {
    /// <summary>
    /// Utilities for simplifying interactions with the NAudio library.
    /// </summary>
    public static class NAudioUtilities {
        /// <summary>
        /// Get the total number of devices according to NAudio's wave functionality.
        /// </summary>
        /// <returns>The number of devices.</returns>
        public static int GetNumberOfDevices() {
            return WaveInterop.waveInGetNumDevs();
        }

        /// <summary>
        /// Retrieves the wave in device.
        /// </summary>
        /// <param name="index">Device to retrieve.</param>
        /// <returns>The WaveIn device capabilities</returns>
        public static WaveInCapabilities GetWaveInDevice(int index) {
            var caps = new WaveInCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr) index, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }
    }
}