using System;
using System.Text.RegularExpressions;
using notification_app.AdministrationFilter;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;

namespace notification_app.Twitch.AdministrationFilter {
    /// <summary>
    ///     Handles banning the "Wanna become famous" bot.
    /// </summary>
    internal class BotWannaBecomeFamous : IAdminFilter {
        /// <summary>
        ///     The persisted configuration.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        public BotWannaBecomeFamous() {
            config = Configuration.Instance();
        }

        /// <summary>
        ///     Handles banning the "Wanna become famous" bot message.
        /// </summary>
        /// <param name="client">The twitch client.</param>
        /// <param name="messageInfo">The information on the chat message.</param>
        public bool handle(TwitchClient client, OnMessageReceivedArgs messageInfo) {
            string chatMessage = messageInfo.ChatMessage.Message;
            if (chatMessage.Contains("Wanna become famous?", StringComparison.InvariantCultureIgnoreCase) &&
                Regex.IsMatch(chatMessage, Constants.REGEX_URL)) {
                client.BanUser(config.TwitchChannel, messageInfo.ChatMessage.Username, "[Bot] Wanna become famous");
                return false;
            }

            return true;
        }
    }
}