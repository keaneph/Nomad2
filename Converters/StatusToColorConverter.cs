using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Nomad2.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "active" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),    // Green
                    "completed" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),  // Blue
                    "overdue" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),     // Red
                    _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))            // Gray (default)
                };
            }

            return new SolidColorBrush(Color.FromRgb(149, 165, 166)); // Default gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}