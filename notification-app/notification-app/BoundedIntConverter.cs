using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace notification_app {
    internal class BoundedIntConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            try {
                return System.Convert.ToInt32(value).ToString();
            } catch (Exception e) {
                return "0";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (null == value)
                return value;

            try {
                return System.Convert.ToInt32(value);
            } catch (Exception e) {
                return 0;
            }
        }
    }
}