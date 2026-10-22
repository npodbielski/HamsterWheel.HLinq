using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public interface IDefaultConverter
{
    /// <summary>
    /// Converts an object to a target type
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="culture">
    /// The culture the text of the value is read in, the invariant one when it is <c>null</c>.
    /// </param>
    T? ConvertTo<T>(object? value, CultureInfo? culture = null);

    /// <summary>
    /// Converts an object to a target type
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="culture">
    /// The culture the text of the value is read in, the invariant one when it is <c>null</c>.
    /// </param>
    /// <param name="targetType">Target type of conversion</param>
    object? ConvertTo(Type targetType, object? value, CultureInfo? culture = null);

    /// <summary>
    /// Converts an object to boolean
    /// </summary>
    /// <param name="arg"></param>
    /// <param name="isNullable"></param>
    bool? ConvertToBoolean(object? arg, bool isNullable);

    /// <summary>
    /// Checks if a value needs to be converted to a specified type
    /// </summary>
    /// <param name="targetType">Target type</param>
    /// <param name="value">Value to convert</param>
    bool NeedConversion(Type targetType, object? value);
}