using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
namespace JLAClient.Converters;
public class StringArrayToString : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string[] strings && targetType.IsAssignableTo(typeof(string)))
        {
            //Convert string[] to a single, formatted string.
            return string.Join(" || ", strings); //using || as my seperator so commas can easily be used in individual strings
        }
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string combinedString && targetType.IsAssignableTo(typeof(string[])))
        {
            //Convert a single, formatted string to string[]
            return combinedString.Split(" || ");
        }
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
}