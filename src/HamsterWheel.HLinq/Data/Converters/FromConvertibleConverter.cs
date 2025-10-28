using System.Globalization;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

public class FromConvertibleConverter : IConfigurableValueConverter
{
    public int Priority => 1002;

    private static readonly Dictionary<Type, Func<IConvertible, object>> ConvertibleMethods =
        new()
        {
            { typeof(int), c => c.ToInt32(CultureInfo.InvariantCulture) },
            { typeof(double), c => c.ToDouble(CultureInfo.InvariantCulture) },
            { typeof(decimal), c => c.ToDecimal(CultureInfo.InvariantCulture) },
            { typeof(string), c => c.ToString(CultureInfo.InvariantCulture) },
            { typeof(byte), c => c.ToByte(CultureInfo.InvariantCulture) },
            { typeof(char), c => c.ToChar(CultureInfo.InvariantCulture) },
            { typeof(short), c => c.ToInt16(CultureInfo.InvariantCulture) },
            { typeof(long), c => c.ToInt64(CultureInfo.InvariantCulture) },
            { typeof(DateTime), c => c.ToDateTime(CultureInfo.InvariantCulture) },
            { typeof(sbyte), c => c.ToSByte(CultureInfo.InvariantCulture) },
            { typeof(ushort), c => c.ToUInt16(CultureInfo.InvariantCulture) },
            { typeof(uint), c => c.ToUInt32(CultureInfo.InvariantCulture) }
        };

    public bool CanConvert(object? value, Type to) =>
        value switch
        {
            IConvertible when ConvertibleMethods.TryGetValue(to, out _) => true,
            _ => false
        };

    public object ConvertTo(object? value, Type to) =>
        value switch
        {
            IConvertible convertible when ConvertibleMethods.TryGetValue(to, out var convertibleFunc) =>
                convertibleFunc(convertible),
            _ => throw new InvalidConstantStringToTypeConversionException(value, to)
        };
}