using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

public class DefaultConverter(
    IEnumerable<IConfigurableValueConverter> converters,
    INullKeyword nullKeyword,
    IFallbackConverter? fallbackSerializer = null) : IDefaultConverter
{
    private readonly IConfigurableValueConverter[] _converters =
        converters.Concat([fallbackSerializer]).Where(c => c is not null).OrderBy(c => c!.Priority).ToArray()!;

    internal static DefaultConverter Instance { get; } =
        new(
        [
            new FromStringConverter(new NullKeyword()), new FromFormattableConverter(), new FromConvertibleConverter(),
            new EnumValueConverter(),
            new NullableEnumValueConverter(new NullKeyword(), new EnumValueConverter()),
            new ToInterfaceConverter(), new ViaSerializationConverter()
        ], new NullKeyword());

    public bool NeedConversion(Type targetType, object? value) => !targetType.IsInstanceOfType(value);

    [return: NotNullIfNotNull(nameof(value))]
    public T? ConvertTo<T>(object? value)
    {
        if (value is T typedValue)
        {
            return typedValue;
        }

        return (T?)ConvertTo(typeof(T), value);
    }

    public object? ConvertTo(Type targetType, object? value)
    {
        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return ConvertToBoolean(value, targetType.IsNullable());
        }

        if (_converters.FirstOrDefault(c => c.CanConvert(value, targetType)) is { } converter)
        {
            return converter.ConvertTo(value, targetType);
        }

        throw new InvalidConstantStringToTypeConversionException(value, targetType);
    }

    public bool? ConvertToBoolean(object? arg, bool isNullable)
    {
        if (nullKeyword.Null.Equals(arg) || arg is null)
        {
            return !isNullable ? false : null;
        }

        switch (arg)
        {
            case bool b:
                return b;
            case char c when char.IsWhiteSpace(c) || c == '\0':
                return false;
            case char c:
                arg = c.ToString();
                break;
        }

        switch (arg)
        {
            case DateTime dt:
                return dt != DateTime.MinValue && dt != DateTime.MaxValue;
            case DateTimeOffset dto:
                return dto != DateTimeOffset.MinValue && dto != DateTimeOffset.MaxValue;
#if !NETSTANDARD
            case TimeOnly to:
                return to != TimeOnly.MinValue && to != TimeOnly.MaxValue;
#endif
            case IConvertible convertible:
                try
                {
                    return convertible.ToBoolean(CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    //swallow and continue with conversion
                }

                break;
        }

        if (arg is not string str)
        {
            return true;
        }

        str = str.ToLower(CultureInfo.InvariantCulture);
        if (str is "0" or "no" or "f" or "n" or "\0")
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(str);

        //at this point it should be not null custom object type -> return true
    }
}