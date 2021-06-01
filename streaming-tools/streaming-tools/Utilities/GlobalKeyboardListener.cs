namespace streaming_tools.Utilities {
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    ///     Listens for keystrokes across the entire OS.
    /// </summary>
    /// <remarks>
    ///     For the string representation of the keys see the mapping in:
    ///     <see href="https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes?redirectedfrom=MSDN" />
    /// </remarks>
    public class GlobalKeyboardListener {
        /// <summary>
        ///     The singleton instance of our class.
        /// </summary>
        private static GlobalKeyboardListener? instance;

        /// <summary>
        ///     The process we launched.
        /// </summary>
        private readonly Process process;

        /// <summary>
        ///     Thread monitoring standard output of <seealso cref="process" /> in order to give the keystrokes.
        /// </summary>
        private readonly Thread thread;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalKeyboardListener" /> class.
        /// </summary>
        protected GlobalKeyboardListener() {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = Constants.WINDOWS_KEYBOARD_HOOK_LOCATION;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.Arguments = Process.GetCurrentProcess().Id.ToString();

            this.process = Process.Start(startInfo);

            this.thread = new Thread(this.ReadKeyboardThread);
            this.thread.IsBackground = true;
            this.thread.Start();
        }

        /// <summary>
        ///     Gets the singleton instance of the class.
        /// </summary>
        public static GlobalKeyboardListener Instance {
            get {
                if (null == instance)
                    instance = new GlobalKeyboardListener();

                return instance;
            }
        }

        /// <summary>
        ///     Gets or sets the callbacks to invoke when a keystroke is pressed.
        /// </summary>
        public Action<string> Callback { get; set; }

        /// <summary>
        ///     The main entry point of the <see cref="thread" /> which listens for output from the
        ///     process.
        /// </summary>
        private void ReadKeyboardThread() {
            while (true) {
                if (null == this.process)
                    return;

                string line;
                while (null != (line = this.process.StandardOutput.ReadLine())) {
                    this.Callback?.Invoke(line);
                }
            }
        }
    }
}