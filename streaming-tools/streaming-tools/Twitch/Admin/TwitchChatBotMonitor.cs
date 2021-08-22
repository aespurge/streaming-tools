namespace streaming_tools.Twitch.Admin {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using Models;
    using Newtonsoft.Json;
    using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
    using TwitchLib.Api.Helix.Models.Users.GetUsers;
    using TwitchLib.Client.Extensions;

    /// <summary>
    ///     Handles monitoring twitch chat for bots.
    /// </summary>
    public class TwitchChatBotMonitor {
        /// <summary>
        ///     For hate bot follows, the maximum number of bots that can be created within the
        ///     <seealso cref="MIN_HOURS_FOLLOWERS_ARE_APART" /> time frame
        ///     before we consider it a hate follow.
        /// </summary>
        private const int MAXIMUM_FOLLOWERS_WITHIN_TIME = 3;

        /// <summary>
        ///     The minimum number of hours each consecutive follower's account must have been created on in order for the two
        ///     accounts to not
        ///     be considered bots that are following.
        /// </summary>
        private const int MIN_HOURS_FOLLOWERS_ARE_APART = 8;

        /// <summary>
        ///     The number of milliseconds to wait between polling for bots.
        /// </summary>
        private const int MONITOR_THREAD_POLL_TIMEOUT = 30000;

        /// <summary>
        ///     The singleton instance of this class.
        /// </summary>
        private static TwitchChatBotMonitor? instance;

        /// <summary>
        ///     The whitelist of bots to not ban.
        /// </summary>
        private static readonly string[] WHITELISTED_BOTS = { "soundalerts", "nightbot", "streamlabs" };

        /// <summary>
        ///     The thread responsible for constantly checking for bots.
        /// </summary>
        private readonly Thread monitorThread;

        /// <summary>
        ///     The list of twitch chats to monitor.
        /// </summary>
        private readonly HashSet<TwitchChatConfiguration> twitchChatsToMonitor;

        /// <summary>
        ///     The poison pill for killing the monitoring thread.
        /// </summary>
        private bool poisonPill;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatBotMonitor" /> class.
        /// </summary>
        protected TwitchChatBotMonitor() {
            this.twitchChatsToMonitor = new HashSet<TwitchChatConfiguration>();
            this.monitorThread = new Thread(this.MonitorForBots);
            this.monitorThread.IsBackground = true;
            this.monitorThread.Name = "Bot Monitor Thread";
            this.monitorThread.Start();
        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static TwitchChatBotMonitor Instance {
            get {
                if (null == TwitchChatBotMonitor.instance) {
                    TwitchChatBotMonitor.instance = new TwitchChatBotMonitor();
                }

                return TwitchChatBotMonitor.instance;
            }
        }

        /// <summary>
        ///     Adds a new chat to monitor for bots.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public void MonitorForBots(TwitchChatConfiguration config) {
            this.twitchChatsToMonitor.Add(config);
        }

        /// <summary>
        ///     Removes a twitch chat from bot monitoring.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public void StopMonitoringForBots(TwitchChatConfiguration config) {
            this.twitchChatsToMonitor.Remove(config);
        }

        /// <summary>
        ///     The main thread that monitors for bots.
        /// </summary>
        private void MonitorForBots() {
            while (!this.poisonPill) {
                try {
                    var allConfigs = this.twitchChatsToMonitor.ToArray();
                    foreach (var config in allConfigs) {
                        if (string.IsNullOrWhiteSpace(config.AccountUsername) || string.IsNullOrWhiteSpace(config.TwitchChannel)) {
                            continue;
                        }

                        if (config.BanBotsInChat) {
                            this.FindBannableBots(config.AccountUsername, config.TwitchChannel);
                        }

                        if (config.BanHateFollowers) {
                            this.FindHateBotFollows(config.AccountUsername, config.TwitchChannel);
                        }
                    }

                    Thread.Sleep(TwitchChatBotMonitor.MONITOR_THREAD_POLL_TIMEOUT);
                } catch (Exception) { }
            }
        }

        /// <summary>
        ///     Finds bots lurking in chat.
        /// </summary>
        /// <param name="admin">The account that is a moderator in the chat.</param>
        /// <param name="channel">The channel to check for bots in chat.</param>
        public async void FindBannableBots(string admin, string channel) {
            var api = await TwitchChatManager.Instance.GetTwitchClientApi(admin);
            if (null == api) {
                return;
            }

            // Reach out to the api and find out what bots are online.
            var http = new HttpClient();
            var response = await http.GetAsync("https://api.twitchinsights.net/v1/bots/online");
            if (!response.IsSuccessStatusCode) {
                return;
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            var jsonString = Encoding.UTF8.GetString(content);
            var liveBotsResponse = JsonConvert.DeserializeObject<TwitchInsightsLiveBotsResponse>(jsonString);
            if (null == liveBotsResponse) {
                return;
            }

            var liveBots = liveBotsResponse.bots.Select(s => s[0].ToString()?.ToLowerInvariant()).Except(TwitchChatBotMonitor.WHITELISTED_BOTS);

            // Get what chatters are in the channel.
            var chattersResponse = await api.Undocumented.GetChattersAsync(channel);
            var chatters = chattersResponse.Select(c => c.Username.ToLowerInvariant());

            // Determine what chatters are bots
            var bots = chatters.Where(c => liveBots.Contains(c));
            var someoneWasBanned = true;
            var client = TwitchChatManager.Instance.GetTwitchChannelClient(channel);
            if (null == client) {
                return;
            }

            // Ban them. The issue with banning is that twitch doesn't always take the command and so we have to try to do it over and over again until we get them all.
            // There is also no API endpoint for seeing another streamers ban list so we have to just "good luck everybody else" it.
            client.OnUserBanned += (sender, args) => someoneWasBanned = true;
            while (someoneWasBanned) {
                someoneWasBanned = false;
                foreach (var user in bots) {
                    while (!client.IsConnected || null == client.JoinedChannels.FirstOrDefault(c => c.Channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))) {
                        Thread.Sleep(100);
                    }

                    try {
                        client.BanUser(channel, user, "bot");
                    } catch (Exception) { }

                    Thread.Sleep(2000);
                }
            }
        }

        /// <summary>
        ///     Reads follower list and determines which followers are likely bots. If there is a trend of bots in succession, bans
        ///     them.
        /// </summary>
        /// <param name="admin">The account that is a moderator in the chat.</param>
        /// <param name="channel">The channel to check for bots in their followers list.</param>
        public async void FindHateBotFollows(string admin, string channel) {
            var api = await TwitchChatManager.Instance.GetTwitchClientApi(admin);
            if (null == api) {
                return;
            }

            // Get the user from the twitch api
            var userInfo = await api.Helix.Users.GetUsersAsync(logins: new List<string> { channel });

            // Get user info for all of the followers of the user.
            var allFollowers = new List<Tuple<Follow, User>>();
            string? cursor = null;
            while (true) {
                var followers = await api.Helix.Users.GetUsersFollowsAsync(toId: userInfo.Users[0].Id, after: cursor, first: 100);
                var followerUserInfo = await api.Helix.Users.GetUsersAsync(followers.Follows.Select(f => f.FromUserId).ToList());

                foreach (var user in followerUserInfo.Users) {
                    var followerInfo = followers.Follows.FirstOrDefault(f => f.FromUserId == user.Id);
                    if (null == followerInfo) {
                        continue;
                    }

                    allFollowers.Add(new Tuple<Follow, User>(followerInfo, user));
                }

                if (null == cursor && null != followers.Pagination.Cursor) {
                    cursor = followers.Pagination.Cursor;
                } else if (null == followers.Pagination.Cursor) {
                    break;
                }

                cursor = followers.Pagination.Cursor;
            }

            // Sort the followers because the API will give it back to us in the wrong order.
            var ban = new List<User>();
            var proposedBans = new List<User>();
            allFollowers.Sort((l, r) => {
                if (l.Item1.FollowedAt > r.Item1.FollowedAt) {
                    return -1;
                }

                if (l.Item1.FollowedAt < r.Item1.FollowedAt) {
                    return 1;
                }

                return 0;
            });

            var allFollowsSorted = new List<User>();
            allFollowsSorted.AddRange(allFollowers.Select(i => i.Item2));

            // Check each follower to see if consecutive accounts were created around the same time. This likely points to someone
            // creating bot accounts for hate follows.
            User lastFollower = allFollowsSorted[0];
            foreach (var follower in allFollowsSorted) {
                // Determine if the followers were created within the same timespan. 
                if (lastFollower.CreatedAt.Day == follower.CreatedAt.Day &&
                    lastFollower.CreatedAt.Month == follower.CreatedAt.Month &&
                    lastFollower.CreatedAt.Year == follower.CreatedAt.Year &&
                    lastFollower.CreatedAt - follower.CreatedAt < new TimeSpan(TwitchChatBotMonitor.MIN_HOURS_FOLLOWERS_ARE_APART, 0, 0)) {
                    if (proposedBans.Count == 0) {
                        proposedBans.Add(lastFollower);
                    }

                    proposedBans.Add(follower);
                } else {
                    // If there were too many consecutive followers within the same time frame, ban them.
                    if (proposedBans.Count >= TwitchChatBotMonitor.MAXIMUM_FOLLOWERS_WITHIN_TIME) {
                        ban.AddRange(proposedBans);
                    }

                    proposedBans.Clear();
                }

                lastFollower = follower;
            }

            // If there's nothing to ban, we're done.
            if (ban.Count == 0) {
                return;
            }

            // Otherwise, grab a twitch client for typing the ban command in chat.
            var someoneWasBanned = true;
            var client = TwitchChatManager.Instance.GetTwitchChannelClient(channel);
            if (null == client) {
                return;
            }

            // Ban them. The issue with banning is that twitch doesn't always take the command and so we have to try to do it over and over again until we get them all.
            // There is also no API endpoint for seeing another streamers ban list so we have to just "good luck everybody else" it.
            client.OnUserBanned += (sender, args) => someoneWasBanned = true;
            while (someoneWasBanned) {
                someoneWasBanned = false;
                foreach (var user in ban) {
                    while (!client.IsConnected || null == client.JoinedChannels.FirstOrDefault(c => c.Channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))) {
                        Thread.Sleep(100);
                    }

                    try {
                        client.BanUser(channel, user.Login, "[Bot] Hate followers");
                    } catch (Exception) { }

                    Thread.Sleep(2000);
                }
            }
        }
    }
}