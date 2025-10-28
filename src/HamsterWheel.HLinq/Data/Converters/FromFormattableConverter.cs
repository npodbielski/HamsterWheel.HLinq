using System.Globalization;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

public class FromFormattableConverter : IConfigurableValueConverter
{
    public int Priority => 1001;

    private static readonly Dictionary<Type, Func<IFormattable, object>> FormattableMethods =
        new()
        {
            { typeof(DateTimeOffset), c => c.ToString("O", CultureInfo.InvariantCulture) },
            { typeof(DateTime), c => c.ToString("O", CultureInfo.InvariantCulture) }
        };

    public bool CanConvert(object? value, Type to) =>
        value switch
        {
            IConvertible when FormattableMethods.TryGetValue(value.GetType(), out _) => true,
            _ => false
        };

    public object ConvertTo(object? value, Type to) =>
        value switch
        {
            IFormattable formattable when FormattableMethods.TryGetValue(value.GetType(), out var formattableFunc) =>
                formattableFunc(formattable),
            _ => throw new InvalidConstantStringToTypeConversionException(value, to)
        };
}