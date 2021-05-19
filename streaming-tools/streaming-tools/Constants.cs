namespace streaming_tools {
    /// <summary>
    ///     Global constants for use in the application.
    /// </summary>
    internal class Constants {
        /// <summary>
        ///     A regular expression for identifying a link.
        /// </summary>
        public const string REGEX_URL = @"(https?:\/\/(www\.)?)+[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=,]*)";

        /// <summary>
        ///     The address to retrieve an OAuth token from twitch.
        /// </summary>
        public const string TWITCH_OAUTH_SITE = @"https://twitchapps.com/tmi/";
    }
}