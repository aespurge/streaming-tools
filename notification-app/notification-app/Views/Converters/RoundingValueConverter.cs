using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace notification_app {
    internal class RoundingValueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return System.Convert.ToInt32(value) + "%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (null == value)
                return value;

            var justNumber = value.ToString().Replace("%", "");
            return System.Convert.ToInt32(justNumber);
        }
    }
}