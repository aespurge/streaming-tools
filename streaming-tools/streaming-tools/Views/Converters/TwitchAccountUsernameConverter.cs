﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace streaming_tools.Views.Converters {
    /// <summary>
    ///     Converts back and forth from Twitch Account objects to a string representation of a username.
    /// </summary>
    internal class TwitchAccountUsernameConverter : IValueConverter {
        /// <summary>
        ///     Converts from a collection of Twitch Account objects to a collection of string usernames.
        /// </summary>
        /// <param name="value">The collection of <seealso cref="TwitchAccount" />.</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>The string representation of a percentage in "#%" format.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var valueCol = value as ICollection<TwitchAccount>;
            if (null == valueCol)
                return null;

            return valueCol.Select(twitchUser => twitchUser.Username).ToList();
        }

        /// <summary>
        ///     Not implemented.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">Not used.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>Absolutely nothing.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}