using System;
using System.Globalization;
using System.Windows.Data;

namespace Nomad2.Converters
{
    public class ViewModelToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.GetType().Name == ((Type)parameter).Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}