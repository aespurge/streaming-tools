using System;
using TwitchLib.Client.Events;

namespace streaming_tools.Twitch.TtsFilter {
    /// <summary>
    ///     A contract for filtering something from TTS.
    /// </summary>
    internal interface ITtsFilter {
        /// <summary>
        ///     Filters a message from TTS.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage);
    }
}