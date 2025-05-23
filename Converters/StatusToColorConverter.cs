using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace Nomad2.Converters
{
    // this converter class changes text status into corresponding colors for visual representation
    public class StatusToColorConverter : IValueConverter
    {
        // converts a status string into a color brush
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // checks if the input value can be treated as a string
            if (value is string status)
            {
                // uses switch expression to match status with corresponding colors
                return status.ToLower() switch
                {
                    "active" => Application.Current.Resources["GreenGradientBackground"],   
                    "completed" => Application.Current.Resources["PurpleGradientBackground"], 
                    "overdue" => Application.Current.Resources["RedGradientBackground"],      
                    _ => Application.Current.Resources["TextSecondaryBrush"]        
                };
            }

            // returns default color if the value is not a string
            return Application.Current.Resources["TextSecondaryBrush"];
        }

        // convert back method is not implemented as it's not needed for this one-way conversion
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}