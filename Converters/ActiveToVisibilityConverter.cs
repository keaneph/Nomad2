using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Nomad2.Converters
{
    // this converter class implements ivalueconverter interface which is used for wpf data binding
    public class ActiveToVisibilityConverter : IValueConverter
    {
        // this method converts a value to visibility enum based on a status string
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // checks if the input value can be treated as a string
            if (value is string status)
            {
                // returns visible if status is "active" (case insensitive), otherwise returns collapsed
                return status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        // convert back method is not implemented as it's not needed for this one-way conversion
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}



//this converter is typically used to control the visibility of UI elements based on a status value.
//basically if the status is "Active", the element will be visible; otherwise, it will be collapsed
//(hidden and takes no space in layout).