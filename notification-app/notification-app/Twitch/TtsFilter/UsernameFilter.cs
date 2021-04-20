using System;
using TwitchLib.Client.Events;

namespace notification_app.Twitch.Filter {
    /// <summary>
    ///     Filters chat messages based on the users that sent them.
    /// </summary>
    internal class UsernameFilter : ITtsFilter {
        /// <summary>
        ///     The users to never read chat messages for.
        /// </summary>
        private readonly string[] ignoreUsers = {
            "streamlabs", "nightbot"
        };

        /// <summary>
        ///     Filters out chat messages for bot users.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message.</returns>
        public string filter(OnMessageReceivedArgs twitchInfo, string currentMessage) {
            foreach (var username in ignoreUsers)
                if (username.Equals(twitchInfo.ChatMessage.DisplayName, StringComparison.InvariantCultureIgnoreCase))
                    return null;

            return currentMessage;
        }
    }
}