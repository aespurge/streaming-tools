namespace streaming_tools {
    using System.Collections.Generic;
    using Avalonia.Controls;
    using Avalonia.Input.Platform;
    using TwitchLib.Api.Core.Enums;

    /// <summary>
    ///     Global constants for use in the application.
    /// </summary>
    internal class Constants {
        /// <summary>
        ///     A regular expression for identifying a link.
        /// </summary>
        public const string REGEX_URL = @"(https?:\/\/(www\.)?)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=,]*)";

        /// <summary>
        ///     The client id of the twitch application.
        /// </summary>
        public const string NULLINSIDE_CLIENT_ID = @"zsvtlgmyl5z0xl5av3i7dexk2rs1hk";

        /// <summary>
        ///     The endpoint that retrieves the URL to generate the "code" to generate an OAuth token.
        /// </summary>
        public const string NULLINSIDE_TWITCH_CODE = @"https://www.nullinside.com/api/v1/twitch/oauth/code";

        /// <summary>
        ///     The endpoint that generates an OAuth token.
        /// </summary>
        public const string NULLINSIDE_TWITCH_OAUTH = @"https://www.nullinside.com/api/v1/twitch/oauth/token";

        /// <summary>
        ///     The endpoint the refreshes an OAuth token.
        /// </summary>
        public const string NULLINSIDE_TWITCH_REFRESH = @"https://www.nullinside.com/api/v1/twitch/oauth/refresh";

        /// <summary>
        ///     The list of authorization scopes we need for the application.
        /// </summary>
        public static readonly IEnumerable<AuthScopes> TWITCH_AUTH_SCOPES = new[] { AuthScopes.Helix_Channel_Manage_Redemptions };

        /// <summary>
        ///     The reference to the main window of the application.
        /// </summary>
        /// <remarks>This is a hack for modal dialogs.</remarks>
        public static Window? MAIN_WINDOW;

        /// <summary>
        ///     The reference to the clipboard API.
        /// </summary>
        /// <remarks>This is a hack because it's hard to get to.</remarks>
        public static IClipboard? CLIPBOARD;

#if DEBUG
        /// <summary>
        ///     The hard coded location for where the keyboard hooks executable lives.
        /// </summary>
        public const string WINDOWS_KEYBOARD_HOOK_LOCATION = @"../../../../WindowsKeyboardHook/bin/Debug/net5.0/WindowsKeyboardHook.exe";
#else
        /// <summary>
        ///     The hard coded location for where the keyboard hooks executable lives.
        /// </summary>
        public const string WINDOWS_KEYBOARD_HOOK_LOCATION = @"../WindowsKeyboardHook/WindowsKeyboardHook.exe";
#endif
    }
}