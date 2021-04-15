using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace notification_app.Twitch.Filter {
    class UsernameFilter : ITtsFilter {
        private string[] ignoreUsers = {
            "streamlabs", "nightbot"
        };

        public string filter(OnMessageReceivedArgs twitchInfo, string currentMessage) {
            foreach (var username in ignoreUsers) {
                if (username.Equals(twitchInfo.ChatMessage.DisplayName, StringComparison.InvariantCultureIgnoreCase))
                    return null;
            }

            return currentMessage;
        }
    }
}