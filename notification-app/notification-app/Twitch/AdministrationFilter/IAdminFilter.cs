using TwitchLib.Client;
using TwitchLib.Client.Events;

namespace notification_app.AdministrationFilter {
    /// <summary>
    ///     Handles administration of the stream.
    /// </summary>
    public interface IAdminFilter {
        /// <summary>
        ///     Handles administration of the chat messages.
        /// </summary>
        /// <param name="client">The twitch client.</param>
        /// <param name="messageInfo">The information on the chat message.</param>
        /// <returns>True if the message is ok, false if it should be ignored.</returns>
        bool handle(TwitchClient client, OnMessageReceivedArgs messageInfo);
    }
}