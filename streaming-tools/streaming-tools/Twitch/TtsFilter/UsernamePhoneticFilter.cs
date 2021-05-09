using System;
using System.Collections.Generic;
using TwitchLib.Client.Events;

namespace streaming_tools.Twitch.TtsFilter {
    /// <summary>
    ///     Converts users names to their phonetic spellings for TTS.
    /// </summary>
    internal class UsernamePhoneticFilter : ITtsFilter {
        /// <summary>
        ///     The hard-coded list of usernames that I know need to be fixed.
        /// </summary>
        private readonly Dictionary<string, string> usernamesToPronunciations = new() {
            {"7gh0sty", "ghosty"},
            {"isdbest", "is-dee-best"},
            {"sk4963", "OxMom"},
            {"gaggablagblag", "Gagga-Blag-Blag"},
            {"viennagaymerbear", "Gaymer-Bear"},
            {"ekamy", "ek-uh-mee "}
        };

        /// <summary>
        ///     Converts a username to it's phonetic spelling for TTS.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        public Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage) {
            string replacementName = usernamesToPronunciations.GetValueOrDefault(twitchInfo.ChatMessage.DisplayName.ToLowerInvariant(), username);

            string message = currentMessage;
            foreach (var usernameToPhonetic in usernamesToPronunciations) message = message.Replace(usernameToPhonetic.Key, usernameToPhonetic.Value, StringComparison.InvariantCultureIgnoreCase);

            return new Tuple<string, string>(replacementName, message);
        }
    }
}