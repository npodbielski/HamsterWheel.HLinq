using System.Globalization;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

public class FromStringConverter(INullKeyword nullKeyword) : IConfigurableValueConverter
{
    public int Priority => 1000;

    private readonly Dictionary<Type, Func<string, object?>> _stringParseMethods =
        new()
        {
            {
                typeof(string),
                s =>
                {
                    if (s == nullKeyword.Null)
                    {
                        return null;
                    }

                    return s.IsDoubleQuoted() ? s.UnQuote() : s;
                }
            },
            {
                typeof(byte),
                s => byte.TryParse(s, out var @byte)
                    ? @byte
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(byte))
            },
            {
                typeof(Guid),
                s =>
                {
                    if (s.IsDoubleQuoted())
                    {
                        s = s.UnQuote();
                    }

                    return Guid.TryParse(s, out var guid)
                        ? guid
                        : throw new InvalidConstantStringToTypeConversionException(s, typeof(Guid));
                }
            },
            {
                typeof(DateTime),
                s => DateTime.TryParse(s, CultureInfo.InvariantCulture, out var dt)
                    ? dt
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(DateTime))
            },
            {
                typeof(DateTimeOffset),
                s => DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                    out var dt)
                    ? dt
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(DateTimeOffset))
            },
            {
                typeof(decimal),
                s => decimal.TryParse(s, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(decimal))
            },
            {
                typeof(double),
                s => double.TryParse(s, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(double))
            },
            {
                typeof(float),
                s => float.TryParse(s, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(float))
            },
            {
                typeof(int),
                s => int.TryParse(s, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(int))
            },
            {
                typeof(long),
                s => long.TryParse(s, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(long))
            },
            {
                typeof(short),
                s => short.TryParse(s, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(short))
            },
            {
                typeof(TimeOnly),
                s => TimeOnly.TryParse(s, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(TimeOnly))
            }
        };

    public bool CanConvert(object? value, Type destination)
    {
        if (value is string)
        {
            return destination.IsNullable() && _stringParseMethods.ContainsKey(destination.GetNullableArgument())
                   || _stringParseMethods.ContainsKey(destination);
        }

        return false;
    }

    public object? ConvertTo(object? value, Type destination)
    {
        if (value is string str)
        {
            if (destination.IsNullable() && (string.IsNullOrWhiteSpace(str) || str == nullKeyword.Null))
            {
                return null;
            }

            return _stringParseMethods[destination.GetNullableArgument()](str);
        }

        throw new InvalidConstantStringToTypeConversionException(value, destination);
    }
}