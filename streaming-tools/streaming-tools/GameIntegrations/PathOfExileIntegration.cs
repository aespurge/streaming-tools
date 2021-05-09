using System;
using System.IO;
using System.Text;
using System.Timers;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace streaming_tools.GameIntegrations {
    /// <summary>
    ///     Integration for the Path of Exile game to increment the death counter when our character dies.
    /// </summary>
    internal class PathOfExileIntegration : IDisposable {
        /// <summary>
        ///     The log poeLogFile containing the client information.
        /// </summary>
        public const string POE_LOG_FILE = @"C:\Program Files (x86)\Steam\steamapps\common\Path of Exile\logs\Client.txt";

        /// <summary>
        ///     The persistent configuration for the application.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     A timer for checking the POE log based on a timer elapsing.
        /// </summary>
        private Timer checkPoeLogTimer;

        /// <summary>
        ///     A reference to the twitch client used to send a message updating the death counter.
        /// </summary>
        private TwitchClient client;

        /// <summary>
        ///     The POE log file opened in memory.
        /// </summary>
        private FileStream poeLogFile;

        /// <summary>
        ///     The stream that passively reads the POE log file.
        /// </summary>
        private StreamReader poeLogFileStream;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PathOfExileIntegration" /> class.
        /// </summary>
        public PathOfExileIntegration() {
            // Grab the configuration
            config = Configuration.Instance();

            // Create the configuration for the twitch client.
            byte[] data = Convert.FromBase64String(config.TwitchOauth);
            string password = Encoding.UTF8.GetString(data);
            ConnectionCredentials credentials = new(config.TwitchUsername, password);
            var clientOptions = new ClientOptions {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            // Connect to the twitch chat.
            WebSocketClient customClient = new(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, config.TwitchChannel);
            client.Connect();

            // Open the log file for streaming and set it to the end of the file.
            poeLogFile = new FileStream(POE_LOG_FILE, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            poeLogFileStream = new StreamReader(poeLogFile);
            poeLogFileStream.BaseStream.Seek(0, SeekOrigin.End);

            // Create the timer for checking the poe log file.
            checkPoeLogTimer = new Timer(5000);
            checkPoeLogTimer.AutoReset = false;
            checkPoeLogTimer.Elapsed += CheckPoeLogTimerElapsed;
            checkPoeLogTimer.Start();
        }

        /// <summary>
        ///     Releases unmanaged resources.
        /// </summary>
        public void Dispose() {
            checkPoeLogTimer.Stop();
            checkPoeLogTimer.Dispose();
            checkPoeLogTimer = null;
            client.Disconnect();
            client = null;
            poeLogFileStream.Dispose();
            poeLogFileStream = null;
            poeLogFile.Dispose();
            poeLogFile = null;
        }

        /// <summary>
        ///     Checks the POE log file to see if the player died.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private void CheckPoeLogTimerElapsed(object sender, ElapsedEventArgs e) {
            try {
                // Read the file until you reach the end.
                while (!poeLogFileStream.EndOfStream) {
                    // Grab the current line
                    var line = poeLogFileStream.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Check if the line contains the string indicating we died.
                    if (!line.Trim().EndsWith("has been slain.", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // If for some reason we aren't connected to twitch chat, connect.
                    if (!client.IsConnected)
                        client.Connect();

                    // Send the message.
                    client.SendMessage(config.TwitchChannel, "!death+");
                }
            } finally {
                // Restart the timer at the end.
                checkPoeLogTimer.Start();
            }
        }
    }
}