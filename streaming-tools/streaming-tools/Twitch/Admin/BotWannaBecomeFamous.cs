namespace streaming_tools.Twitch.Admin {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;

    using TwitchLib.Client;
    using TwitchLib.Client.Events;
    using TwitchLib.Client.Extensions;

    /// <summary>
    ///     Handles banning the "Wanna become famous" bot.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    internal class BotWannaBecomeFamous : IAdminFilter {
        /// <summary>
        ///     Handles banning the "Wanna become famous" bot message.
        /// </summary>
        /// <param name="config">The configuration for the twitch chat.</param>
        /// <param name="client">The twitch client.</param>
        /// <param name="messageInfo">The information on the chat message.</param>
        /// <returns>True if the message should be passed on, false if it should be discarded.</returns>
        public bool Handle(TwitchChatConfiguration config, TwitchClient client, OnMessageReceivedArgs messageInfo) {
            string chatMessage = messageInfo.ChatMessage.Message;
            if (chatMessage.Contains("Wanna become famous?", StringComparison.InvariantCultureIgnoreCase) && Regex.IsMatch(chatMessage, Constants.REGEX_URL)) {
                client.BanUser(config.TwitchChannel, messageInfo.ChatMessage.Username, "[Bot] Wanna become famous");
                return false;
            }

            return true;
        }
    }
}