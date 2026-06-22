using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Project.Converters
{
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
                return !string.IsNullOrWhiteSpace(text) && text != "—";
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}