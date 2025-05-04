using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace VoiceAssistant.UI.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            // Green when listening, gray when idle
            return isActive 
                ? new SolidColorBrush(Color.Parse("#4CAF50")) // Green
                : new SolidColorBrush(Color.Parse("#9E9E9E")); // Gray
        }
        
        return new SolidColorBrush(Color.Parse("#9E9E9E")); // Gray by default
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 