﻿// <auto-generated/>
namespace streaming_tools.Models {
    using System.Collections.Generic;

    /// <summary>
    /// The response from twitch for an OAuth token.
    /// </summary>
    public class TwitchTokenResponseJson {
        /// <summary>
        /// The OAuth token.
        /// </summary>
        public string access_token { get; set; }

        /// <summary>
        /// The expire date.
        /// </summary>
        public int expires_in { get; set; }

        /// <summary>
        /// The refresh token.
        /// </summary>
        public string refresh_token { get; set; }

        /// <summary>
        /// The scope.
        /// </summary>
        public List<string> scope { get; set; }

        /// <summary>
        /// The token type.
        /// </summary>
        public string token_type { get; set; }
    }
}