using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace HardwareUsageMonitor.Converters
{
    public class UsageToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double or int or float)
            {
                double val = System.Convert.ToDouble(value);

                if (val < 50) return Brushes.LimeGreen;
                if (val < 80) return Brushes.Gold;
                return Brushes.IndianRed;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}