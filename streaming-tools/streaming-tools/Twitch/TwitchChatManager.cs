﻿namespace streaming_tools.Twitch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using TwitchLib.Client;
    using TwitchLib.Client.Events;
    using TwitchLib.Client.Models;
    using TwitchLib.Communication.Clients;
    using TwitchLib.Communication.Models;

    /// <summary>
    ///     Organizes and aggregates the clients connected to zero or more twitch chats. Invokes callbacks for messages
    ///     received
    ///     in chats.
    /// </summary>
    public class TwitchChatManager {
        /// <summary>
        ///     The singleton instance of the class.
        /// </summary>
        private static TwitchChatManager? instance;

        /// <summary>
        ///     The mapping of twitch clients to their requested configurations.
        /// </summary>
        private readonly Dictionary<TwitchClient, TwitchConnection?> twitchClients = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="TwitchChatManager" /> class.
        /// </summary>
        /// <remarks>This is protected to prevent instantiation outside of our singleton.</remarks>
        protected TwitchChatManager() { }

        /// <summary>
        ///     Gets the singleton instance of the class.
        /// </summary>
        public static TwitchChatManager Instance {
            get {
                if (null == instance)
                    instance = new TwitchChatManager();

                return instance;
            }
        }

        /// <summary>
        ///     Adds a callback from receiving twitch chat messages.
        /// </summary>
        /// <param name="account">The account to connect with.</param>
        /// <param name="channel">The name of the channel to join.</param>
        /// <param name="messageCallback">The callback to invoke when a message is received.</param>
        public void AddTwitchChannel(TwitchAccount? account, string? channel, Action<TwitchClient, OnMessageReceivedArgs>? messageCallback) {
            if (null == account || null == channel || null == messageCallback)
                return;

            var conn = this.GetOrCreateConnection(account, channel);
            if (null == conn)
                return;

            conn.MessageCallbacks += messageCallback;
        }

        /// <summary>
        ///     Adds a callback to perform administrative functions on twitch chat (e.g. like banning users) and optionally
        ///     preventing messages
        ///     from being propagated to other callbacks.
        /// </summary>
        /// <param name="account">The account to connect with.</param>
        /// <param name="channel">The name of the channel to join.</param>
        /// <param name="adminCallback">The callback to invoke when a message is received.</param>
        public void AddTwitchChannelAdminFilter(TwitchAccount? account, string? channel, Func<TwitchClient, OnMessageReceivedArgs, bool>? adminCallback) {
            if (null == account || null == channel || null == adminCallback)
                return;

            var conn = this.GetOrCreateConnection(account, channel);
            if (null == conn)
                return;

            conn.AdminCallbacks += adminCallback;
        }

        /// <summary>
        ///     Remove a callback from receiving twitch chat messages.
        /// </summary>
        /// <param name="account">The account originally subscribed with.</param>
        /// <param name="channel">The name of the channel that was joined.</param>
        /// <param name="messageCallback">The callback to remove.</param>
        public void RemoveTwitchChannel(TwitchAccount? account, string? channel, Action<TwitchClient, OnMessageReceivedArgs>? messageCallback) {
            if (null == account || null == channel || null == messageCallback)
                return;

            var allExisting = from connection in this.twitchClients
                              where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                              select connection;

            foreach (var existing in allExisting.ToArray()) {
                existing.Value.MessageCallbacks -= messageCallback;

                if (null == existing.Value.MessageCallbacks && null == existing.Value.AdminCallbacks) {
                    this.twitchClients.Remove(existing.Key);
                    existing.Key.Disconnect();
                }
            }
        }

        /// <summary>
        ///     Remove a callback from administering twitch chat.
        /// </summary>
        /// <param name="account">The account originally subscribed with.</param>
        /// <param name="channel">The name of the channel that was joined.</param>
        /// <param name="adminCallback">The callback to remove.</param>
        public void RemoveTwitchChannelAdminFilter(TwitchAccount? account, string? channel, Func<TwitchClient, OnMessageReceivedArgs, bool>? adminCallback) {
            if (null == account || null == channel || null == adminCallback)
                return;

            var allExisting = from connection in this.twitchClients
                              where connection.Value.Account == account && connection.Value.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                              select connection;

            foreach (var existing in allExisting.ToArray()) {
                if (null != existing.Value?.AdminCallbacks)
                    existing.Value.AdminCallbacks -= adminCallback;

                if (null == existing.Value?.MessageCallbacks && null == existing.Value?.AdminCallbacks) {
                    this.twitchClients.Remove(existing.Key);
                    existing.Key.Disconnect();
                }
            }
        }

        /// <summary>
        ///     Gets or creates a new connection to a twitch chat.
        /// </summary>
        /// <param name="account">The account to connect with.</param>
        /// <param name="channel">The twitch channel to connect to.</param>
        /// <returns>An instance of the twitch connection.</returns>
        private TwitchConnection GetOrCreateConnection(TwitchAccount account, string channel) {
            var existing = from connection in this.twitchClients.Values
                           where connection.Account == account && connection.Channel?.Equals(channel, StringComparison.InvariantCultureIgnoreCase) == true
                           select connection;

            if (existing.Any())
                return existing.First();

            var conn = new TwitchConnection() { Account = account, Channel = channel };

            string? password = (null != account.OAuth) ? Encoding.UTF8.GetString(Convert.FromBase64String(account.OAuth)) : null;
            var credentials = new ConnectionCredentials(account.Username, password);
            var clientOptions = new ClientOptions { MessagesAllowedInPeriod = 750, ThrottlingPeriod = TimeSpan.FromSeconds(30) };

            WebSocketClient customClient = new(clientOptions);
            var twitchClient = new TwitchClient(customClient);
            twitchClient.Initialize(credentials, channel);

            twitchClient.OnMessageReceived += this.TwitchClient_OnMessageReceived;
            twitchClient.Connect();
            this.twitchClients[twitchClient] = conn;

            return conn;
        }

        /// <summary>
        /// The callback invoked when a message is received in twitch chat.
        /// </summary>
        /// <param name="sender">The twitch chat client.</param>
        /// <param name="e">The message information.</param>
        private void TwitchClient_OnMessageReceived(object? sender, OnMessageReceivedArgs e) {
            if (null == sender)
                return;

            var twitchClient = sender as TwitchClient;
            if (null == twitchClient)
                return;

            var conn = this.twitchClients.GetValueOrDefault(twitchClient, null);
            if (null == conn)
                return;

            if (null != conn.AdminCallbacks) {
                foreach (var adminFilter in conn.AdminCallbacks.GetInvocationList()) {
                    var shouldContinue = (bool)(adminFilter.DynamicInvoke(twitchClient, e) ?? true);
                    if (!shouldContinue)
                        return;
                }
            }

            conn.MessageCallbacks?.Invoke(twitchClient, e);
        }

        /// <summary>
        /// A mapping of all information related to a single twitch chat connection.
        /// </summary>
        private class TwitchConnection {
            /// <summary>
            /// Gets or sets the account connected with.
            /// </summary>
            public TwitchAccount? Account { get; set; }

            /// <summary>
            /// Gets or sets the channel connected to.
            /// </summary>
            public string? Channel { get; set; }

            /// <summary>
            /// Gets or sets the callbacks used to administrate the twitch chat.
            /// </summary>
            public Func<TwitchClient, OnMessageReceivedArgs, bool>? AdminCallbacks { get; set; }

            /// <summary>
            /// Gets or sets the callbacks to handle chat messages.
            /// </summary>
            public Action<TwitchClient, OnMessageReceivedArgs>? MessageCallbacks { get; set; }
        }
    }
}