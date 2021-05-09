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
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message.</returns>
        string Filter(OnMessageReceivedArgs twitchInfo, string currentMessage);
    }
}