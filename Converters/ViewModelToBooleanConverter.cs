using System;
using System.Globalization;
using System.Windows.Data;

namespace Nomad2.Converters
{
    // this converter checks if a viewmodel matches a specific type and returns a boolean
    public class ViewModelToBooleanConverter : IValueConverter
    {
        // converts from a viewmodel object to a boolean by comparing type names
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // if value null or para, return false
            if (value == null || parameter == null) return false;
            // if value matches the parameter type name, return true
            return value.GetType().Name == ((Type)parameter).Name;
        }

        // converts back from boolean to viewmodel; not used in this case
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}

// notes: shows/hides ui elements based on the viewmodel type; this would make the button visible only when the DataContext is of type "SpecificViewModel".
//<Button IsVisible="{Binding DataContext, Converter ={ StaticResource ViewModelToBooleanConverter}, ConverterParameter ={ x: Type local:SpecificViewModel}}"/>