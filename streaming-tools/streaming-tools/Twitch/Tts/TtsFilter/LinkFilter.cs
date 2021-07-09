﻿namespace streaming_tools.Twitch.Tts.TtsFilter {
    using System;
    using System.Text.RegularExpressions;
    using TwitchLib.Client.Events;

    /// <summary>
    ///     Filters out links from being read.
    /// </summary>
    internal class LinkFilter : ITtsFilter {
        /// <summary>
        ///     Filters out links from text to speech.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        public Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage) {
            return new Tuple<string, string>(username, Regex.Replace(currentMessage, Constants.REGEX_URL, string.Empty));
        }
    }
}