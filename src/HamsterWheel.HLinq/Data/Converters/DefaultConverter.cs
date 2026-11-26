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
        [.. converters.Concat([fallbackSerializer]).Where(c => c is not null).OrderBy(c => c!.Priority)!];

    /// <summary>
    /// The standard converter set, reading a written value the way a filter reads it.
    /// </summary>
    public static DefaultConverter Instance { get; } = new(GetDefaultConverters(), new NullKeyword());

    /// <summary>
    /// The converters of the standard set, in the order their <see cref="IConfigurableValueConverter.Priority" />
    /// puts them into a <see cref="DefaultConverter" />.
    /// </summary>
    public static IEnumerable<IConfigurableValueConverter> GetDefaultConverters()
    {
        return
        [
            new FromStringConverter(new NullKeyword()), new FromFormattableConverter(), new FromConvertibleConverter(),
            new Iso8601TimeSpanConverter(),
            new EnumValueConverter(),
            new NullableEnumValueConverter(new NullKeyword(), new EnumValueConverter()),
            new CustomTypeCombinationsConverter(),
            new ToInterfaceConverter(), new ViaSerializationConverter()
        ];
    }

    public bool NeedConversion(Type targetType, object? value)
    {
        if (targetType.IsClass && value == null)
        {
            return false;
        }

        return !targetType.IsInstanceOfType(value);
    }

    [return: NotNullIfNotNull(nameof(value))]
    public T? ConvertTo<T>(object? value, CultureInfo? culture = null)
    {
        if (value is T typedValue and not string)
        {
            return typedValue;
        }

        return (T?)ConvertTo(typeof(T), value, culture);
    }

    public object? ConvertTo(Type targetType, object? value, CultureInfo? culture = null)
    {
        //leave unquoting to the converter if the target is a string
        if (targetType.GetNullableArgument() != typeof(string))
        {
            value = ReadQuotedValue(value);
        }

        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return ConvertToBoolean(value, targetType.IsNullable());
        }

        //a bare four digit number compared to a date is the year the engine reads when it reads
        //a date: the number is read through the chain itself, so a host that reads an int its own
        //way has said the year its own way too
        if (IsDate(targetType) && value is string yearText && IsYear(yearText))
        {
            var year = ConvertTo<int>(yearText, culture);
            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                return new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            return new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }

        foreach (var converter in _converters)
        {
            if (converter.TryConvert(value, targetType, culture) is (true, var result))
            {
                return result;
            }
        }

        throw new InvalidConstantStringToTypeConversionException(value, targetType);
    }

    public bool? ConvertToBoolean(object? arg, bool isNullable)
    {
        if (nullKeyword.Value.Equals(arg) || arg is null)
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
    private static bool IsDate(Type type) =>
        type == typeof(DateTime) || type == typeof(DateTime?) || type == typeof(DateTimeOffset)
        || type == typeof(DateTimeOffset?);

    //four digits that are a year of a date, no sign and no separator: what reads back as the
    //same year whole is all a year can be written as
    private static bool IsYear(string text) =>
        text.Length == 4 && text.All(char.IsDigit) && int.Parse(text) >= 1;

    private object? ReadQuotedValue(object? value) =>
        value is string text && text.IsDoubleQuoted() && text.UnQuote() != nullKeyword.Value
            ? text.UnQuote()
            : value;

}
