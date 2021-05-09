﻿using System.Text.RegularExpressions;
using TwitchLib.Client.Events;

namespace streaming_tools.Twitch.TtsFilter {
    /// <summary>
    ///     Filters out links from twitch chat.
    /// </summary>
    internal class LinkFilter : ITtsFilter {
        /// <summary>
        ///     Filters out links from text to speech.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The updated string that text to speech should read.</returns>
        public string Filter(OnMessageReceivedArgs twitchInfo, string currentMessage) {
            return Regex.Replace(currentMessage, Constants.REGEX_URL, string.Empty);
        }
    }
}