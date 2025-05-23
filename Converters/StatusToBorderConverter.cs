using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace Nomad2.Converters
{
    public class StatusToBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "active" => Application.Current.Resources["GreenGradientBorder"],   
                    "completed" => Application.Current.Resources["PurpleGradientBorder"], 
                    "overdue" => Application.Current.Resources["RedGradientBorder"],      
                    _ => Application.Current.Resources["BorderBrush"]        
                };
            }

            return Application.Current.Resources["BorderBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 