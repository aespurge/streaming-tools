using System;
using System.Runtime.InteropServices;
using NAudio;
using NAudio.Wave;

namespace streaming_tools.Utilities {
    /// <summary>
    ///     Utilities for simplifying interactions with the NAudio library.
    /// </summary>
    public static class NAudioUtilities {
        /// <summary>
        ///     Get the total number of devices according to NAudio's wave functionality.
        /// </summary>
        /// <returns>The number of devices.</returns>
        public static int GetTotalInputDevices() {
            return WaveInterop.waveInGetNumDevs();
        }

        /// <summary>
        ///     Retrieves the input device.
        /// </summary>
        /// <param name="index">Device to retrieve.</param>
        /// <returns>The input device capabilities.</returns>
        public static WaveInCapabilities GetInputDevice(int index) {
            var caps = new WaveInCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr) index, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        /// <summary>
        ///     Returns the number of output devices available in the system.
        /// </summary>
        /// <remarks>Add two to the end to get all devices?</remarks>
        public static int GetTotalOutputDevices() {
            return WaveInterop.waveOutGetNumDevs();
        }

        /// <summary>
        ///     Retrieves the output device.
        /// </summary>
        /// <param name="index">The index of the device.</param>
        /// <returns>The output device capabilities.</returns>
        public static WaveOutCapabilities GetOutputDevice(int index) {
            var caps = new WaveOutCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr) index, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }
    }
}