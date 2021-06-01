namespace streaming_tools.GameIntegrations {
    using System;
    using System.Timers;

    /// <summary>
    ///     Integration for the Path of Exile game to increment the death counter when our character dies.
    /// </summary>
    internal class PathOfExileIntegration : IDisposable {
        ///// <summary>
        /////     The log poeLogFile containing the client information.
        ///// </summary>
        //public const string POE_LOG_FILE = @"C:\Program Files (x86)\Steam\steamapps\common\Path of Exile\logs\Client.txt";

        ///// <summary>
        /////     The persistent configuration for the application.
        ///// </summary>
        //private readonly Configuration config;

        ///// <summary>
        /////     A timer for checking the POE log based on a timer elapsing.
        ///// </summary>
        //private Timer? checkPoeLogTimer;

        ///// <summary>
        /////     A reference to the twitch client used to send a message updating the death counter.
        ///// </summary>
        //private TwitchClient? client;

        ///// <summary>
        /////     The POE log file opened in memory.
        ///// </summary>
        //private FileStream? poeLogFile;

        ///// <summary>
        /////     The stream that passively reads the POE log file.
        ///// </summary>
        //private StreamReader? poeLogFileStream;

        /// <summary>
        ///     Releases unmanaged resources.
        /// </summary>
        public void Dispose() {
            //this.checkPoeLogTimer?.Stop();
            //this.checkPoeLogTimer?.Dispose();
            //this.checkPoeLogTimer = null;
            //this.client?.Disconnect();
            //this.client = null;
            //this.poeLogFileStream?.Dispose();
            //this.poeLogFileStream = null;
            //this.poeLogFile?.Dispose();
            //this.poeLogFile = null;
        }

        /// <summary>
        ///     Checks the POE log file to see if the player died.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private void CheckPoeLogTimerElapsed(object sender, ElapsedEventArgs e) {
            //try {
            //    // Read the file until you reach the end.
            //    while (!poeLogFileStream.EndOfStream) {
            //        // Grab the current line
            //        var line = poeLogFileStream.ReadLine();
            //        if (string.IsNullOrWhiteSpace(line))
            //            continue;

            //        // Check if the line contains the string indicating we died.
            //        if (!line.Trim().EndsWith("has been slain.", StringComparison.InvariantCultureIgnoreCase))
            //            continue;

            //        // If for some reason we aren't connected to twitch chat, connect.
            //        if (!client.IsConnected)
            //            client.Connect();

            //        // Send the message.
            //        client.SendMessage(config.TwitchChannel, "!death+");
            //    }
            //} finally {
            //    // Restart the timer at the end.
            //    checkPoeLogTimer.Start();
            //}
        }
    }
}