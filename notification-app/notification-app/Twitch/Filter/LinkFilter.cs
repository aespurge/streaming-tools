using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace notification_app.Twitch.Filter
{
    /// <summary>
    /// Filters out links from twitch chat.
    /// </summary>
    class LinkFilter : ITtsFilter
    {
        /// <summary>
        /// Filters out links from text to speech.
        /// </summary>
        /// &lt;param name="twitchInfo"&gt;The information on the original chat message.&lt;/param&gt;
        /// /// &lt;param name="currentMessage"&gt;The message from twitch chat.&lt;/param&gt;
        /// <returns>The updated string that text to speech should read.</returns>
        public string filter(OnMessageReceivedArgs twitchInfo, string currentMessage) {
            string pattern = @"(https?:\/\/(www\.)?)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=,]*)";
            return Regex.Replace(currentMessage, pattern, String.Empty);
        }
    }
}
