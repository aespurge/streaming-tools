namespace streaming_tools.Twitch.Tts.TtsFilter {
    using System;
    using System.Collections.Generic;

    using TwitchLib.Client.Events;

    /// <summary>
    ///     Converts users names to their phonetic spellings.
    /// </summary>
    internal class UsernamePhoneticFilter : ITtsFilter {
        /// <summary>
        ///     The hard-coded list of usernames that I know need to be fixed.
        /// </summary>
        private readonly Dictionary<string, string> usernamesToPronunciations = new() {
                                                                                          { "7gh0sty", "ghosty" },
                                                                                          { "isdbest", "is-dee-best" },
                                                                                          { "sk4963", "OxMom" },
                                                                                          { "gaggablagblag", "Gagga-Blag-Blag" },
                                                                                          { "viennagaymerbear", "Gaymer-Bear" },
                                                                                          { "ekamy", "eckahhmee" },
                                                                                          { "vtleavs", "v t levs" },
                                                                                          { "impicusmaximus", "Impicus" },
                                                                                          { "lonkwore", "lonk" },
                                                                                          { "yahya11419", "yah yah" },
                                                                                          { "AresWyler", "aireese" },
                                                                                          { "roxanepigiste", "RocksAnnePeegeest" },
                                                                                          { "alphastar592004", "AlphaStar" },
                                                                                          { "derpidyderp", "Derpity Derp" }
                                                                                      };

        /// <summary>
        ///     Converts a username to it's phonetic spelling for TTS.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="username">The username of the twitch chatter for TTS to say.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message and username.</returns>
        public Tuple<string, string> Filter(OnMessageReceivedArgs twitchInfo, string username, string currentMessage) {
            string replacementName = this.usernamesToPronunciations.GetValueOrDefault(twitchInfo.ChatMessage.DisplayName.ToLowerInvariant(), username);

            string message = currentMessage;
            foreach (var usernameToPhonetic in this.usernamesToPronunciations)
                message = message.Replace(usernameToPhonetic.Key, usernameToPhonetic.Value, StringComparison.InvariantCultureIgnoreCase);

            return new Tuple<string, string>(replacementName, message);
        }
    }
}