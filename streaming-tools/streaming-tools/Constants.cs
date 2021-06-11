namespace streaming_tools {
    using System.Collections.Generic;

    using Avalonia.Controls;

    using TwitchLib.Api.Core.Enums;

    /// <summary>
    ///     Global constants for use in the application.
    /// </summary>
    internal class Constants {
        /// <summary>
        ///     A regular expression for identifying a link.
        /// </summary>
        public const string REGEX_URL = @"(https?:\/\/(www\.)?)+[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=,]*)";

        /// <summary>
        ///     The address to retrieve an OAuth token from twitch.
        /// </summary>
        public const string TWITCH_CHAT_OAUTH_SITE = @"https://twitchapps.com/tmi/";

        /// <summary>
        ///     The address to get a client id.
        /// </summary>
        public const string TWITCH_DEVELOPER_SITE = @"https://dev.twitch.tv/console/apps";

        /// <summary>
        ///     The list of authorization scopes we need for the application.
        /// </summary>
        public static readonly IEnumerable<AuthScopes> TWITCH_AUTH_SCOPES = new[] { AuthScopes.Helix_Channel_Read_Redemptions };

        /// <summary>
        ///     The reference to the main window of the application.
        /// </summary>
        /// <remarks>This is a hack for modal dialogs.</remarks>
        public static Window? MAIN_WINDOW;

#if DEBUG
        /// <summary>
        ///     The hard coded location for where the keyboard hooks executable lives.
        /// </summary>
        public const string WINDOWS_KEYBOARD_HOOK_LOCATION = @"../../../../WindowsKeyboardHook/bin/Debug/net5.0/WindowsKeyboardHook.exe";
#else
        /// <summary>
        /// The hard coded location for where the keyboard hooks executable lives.
        /// </summary>
        public const string WINDOWS_KEYBOARD_HOOK_LOCATION = @"../../../../WindowsKeyboardHook/bin/Release/net5.0/WindowsKeyboardHook.exe";
#endif
    }
}