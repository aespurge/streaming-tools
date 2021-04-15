using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace notification_app.Twitch.Filter
{
    /// <summary>
    /// A contract for filtering something from TTS.
    /// </summary>
    interface ITtsFilter {
        /// <summary>
        /// Filters a message from TTS.
        /// </summary>
        /// <param name="twitchInfo">The information on the original chat message.</param>
        /// <param name="currentMessage">The message from twitch chat.</param>
        /// <returns>The new TTS message.</returns>
        string filter(OnMessageReceivedArgs twitchInfo, string currentMessage);
    }
}
