using System;
using System.Runtime.InteropServices;
using NAudio;
using NAudio.Wave;

namespace notification_app {
    public static class NAudioUtilities {
        public static int GetNumberOfDevices() {
            return WaveInterop.waveInGetNumDevs();
        }

        /// <summary>
        ///     Retrieves the capabilities of a waveIn device
        /// </summary>
        /// <param name="devNumber">Device to test</param>
        /// <returns>The WaveIn device capabilities</returns>
        public static WaveInCapabilities GetCapabilities(int devNumber) {
            var caps = new WaveInCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr) devNumber, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }
    }
}