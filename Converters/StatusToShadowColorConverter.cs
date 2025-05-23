using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace Nomad2.Converters
{
    public class StatusToShadowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "active" => Application.Current.Resources["GreenColor"],   
                    "completed" => Application.Current.Resources["PurpleColor"], 
                    "overdue" => Application.Current.Resources["RedColor"],      
                    _ => Application.Current.Resources["BorderColor"]        
                };
            }

            return Application.Current.Resources["BorderColor"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 