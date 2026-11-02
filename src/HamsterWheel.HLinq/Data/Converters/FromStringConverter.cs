using System.Globalization;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

public class FromStringConverter(INullKeyword nullKeyword) : IConfigurableValueConverter, IWrittenValueParser
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
            {
                typeof(TimeSpan),
                (s, culture) => TimeSpan.TryParse(s, culture, out var span)
                    ? span
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(TimeSpan))
            },
#if !NETSTANDARD
            {
                typeof(DateOnly),
                (s, culture) => DateOnly.TryParse(s, culture, out var date)
                    ? date
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(DateOnly))
            },
            {
                typeof(TimeOnly),
                (s, culture) => TimeOnly.TryParse(s, culture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(TimeOnly))
            }
#endif
        };

    /// <summary>
    /// Says whether a text a query writes parses as a value of the type, in the culture the query
    /// is written in, with quotes stripped and the null keyword of this converter honoured - the
    /// question "what may a query write" asked of the registered instance, so the overload ranking
    /// and the argument conversion read the same table and cannot drift apart.
    /// </summary>
    public bool Parses(string text, Type destination, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(text)
            || !_stringParseMethods.TryGetValue(destination.GetNullableArgument(), out var parse))
        {
            return false;
        }

        var value = text.IsDoubleQuoted() ? text.UnQuote() : text;
        if (destination.GetNullableArgument() == typeof(char) && value.Length != 1 && !value.IsSingleQuoted())
        {
            //the char entry reads the first character of anything longer, which is a value of two
            //characters, not a refusal - a char is one character, quoted or not
            return false;
        }

        if (destination.IsNullable() &&
            (string.IsNullOrWhiteSpace(value) || value == nullKeyword.Value))
        {
            return true;
        }

        try
        {
            parse(value, culture ?? CultureInfo.InvariantCulture);
            return true;
        }
        catch (InvalidConstantStringToTypeConversionException)
        {
            return false;
        }
    }

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