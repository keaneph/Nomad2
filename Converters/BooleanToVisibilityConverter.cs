using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Nomad2.Converters
{
    // this converter class transforms between boolean values and WPF visibility values
    public class BooleanToVisibilityConverter : IValueConverter
    {
        // converts from boolean to visibility (used when binding from source to target)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // checks if value is boolean; checks if true; if true, returns Visible; if false, returns Collapsed
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        // from target to source, converts visibility to boolean; 
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // checks if the input is a visibility enum
            if (value is Visibility visibility)
            {
                // returns true if visible; false if not
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}

// comments: shows/hides an element based on a boolean value in viewmodel. 
// <Button Visibility="{Binding IsEnabled, Converter={StaticResource BooleanToVisibilityConverter}}"/>