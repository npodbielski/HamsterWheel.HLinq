using System.Globalization;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

public class FromStringConverter(INullKeyword nullKeyword) : IConfigurableValueConverter
{
    public int Priority => 1000;

    private const NumberStyles IntegralNumberStyles = NumberStyles.AllowLeadingWhite
                                                      | NumberStyles.AllowTrailingWhite
                                                      | NumberStyles.AllowLeadingSign
                                                      | NumberStyles.AllowTrailingSign
                                                      | NumberStyles.AllowThousands;

    private const NumberStyles FloatingNumberStyles = NumberStyles.AllowLeadingWhite
                                                      | IntegralNumberStyles
                                                      | NumberStyles.AllowDecimalPoint
                                                      | NumberStyles.AllowExponent;

    private readonly Dictionary<Type, Func<string, CultureInfo, object?>> _stringParseMethods =
        new()
        {
            {
                typeof(string),
                (s, _) =>
                {
                    if (s == nullKeyword.Value)
                    {
                        return null;
                    }

                    return s.IsDoubleQuoted() ? s.UnQuote() : s;
                }
            },
            {
                typeof(char),
                (s, _) =>
                {
                    if (s == nullKeyword.Value)
                    {
                        return null;
                    }

                    return s.IsSingleQuoted() ? s.UnQuote() : s[0];
                }
            },
            {
                typeof(byte),
                (s, culture) => byte.TryParse(s, IntegralNumberStyles, culture, out var @byte)
                    ? @byte
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(byte))
            },
            {
                typeof(Guid),
                (s, _) => Guid.TryParse(s, out var guid)
                    ? guid
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(Guid))
            },
            {
                typeof(DateTime),
                (s, culture) => DateTime.TryParse(s, culture, DateTimeStyles.AssumeUniversal, out var dt)
                    ? dt.ToUniversalTime()
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(DateTime))
            },
            {
                typeof(DateTimeOffset),
                (s, culture) => DateTimeOffset.TryParse(s, culture, DateTimeStyles.AssumeUniversal, out var dt)
                    ? dt
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(DateTimeOffset))
            },
            {
                typeof(decimal),
                (s, culture) => decimal.TryParse(s, FloatingNumberStyles, culture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(decimal))
            },
            {
                typeof(double),
                (s, culture) => double.TryParse(s, FloatingNumberStyles, culture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(double))
            },
            {
                typeof(float),
                (s, culture) => float.TryParse(s, FloatingNumberStyles, culture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(float))
            },
            {
                typeof(int),
                (s, culture) => int.TryParse(s, IntegralNumberStyles, culture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(int))
            },
            {
                typeof(long),
                (s, culture) => long.TryParse(s, IntegralNumberStyles, culture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(long))
            },
            {
                typeof(short),
                (s, culture) => short.TryParse(s, IntegralNumberStyles, culture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(short))
            },
#if !NETSTANDARD
            {
                typeof(TimeOnly),
                (s, culture) => TimeOnly.TryParse(s, culture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(TimeOnly))
            }
#endif
        };

    public (bool success, object? result) TryConvert(object? value, Type destination,
        CultureInfo? culture = null)
    {
        if (value is null)
        {
            return (true, destination.IsNullable() ? null : Activator.CreateInstance(destination));
        }

        if (value is not string text
            || !_stringParseMethods.TryGetValue(destination.GetNullableArgument(), out var parse))
        {
            return (false, null);
        }

        return destination.IsNullable() &&
               (string.IsNullOrWhiteSpace(text) || text == nullKeyword.Value)
            ? (true, null)
            : (true, parse(text, culture ?? CultureInfo.InvariantCulture));
    }
}