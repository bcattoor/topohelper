using System;
using System.Windows;
using System.Windows.Data;

namespace TopoHelper.UserControls.Converters
{
    internal class BoolToVisibilityConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return bool.Parse(value.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch ((Visibility)value)
            {
                case Visibility.Collapsed:
                    {
                        return false;
                    }
                case Visibility.Hidden:
                    {
                        return false;
                    }
                default:
                    {
                        return true;
                    }
            }
        }
    }
}