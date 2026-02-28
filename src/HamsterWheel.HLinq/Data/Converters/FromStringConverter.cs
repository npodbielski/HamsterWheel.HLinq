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
                    if (s == nullKeyword.Value)
                    {
                        return null;
                    }

                    return s.IsDoubleQuoted() ? s.UnQuote() : s;
                }
            },
            {
                typeof(char),
                s =>
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
                s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
                    ? dt.ToUniversalTime()
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
                s => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(decimal))
            },
            {
                typeof(double),
                s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(double))
            },
            {
                typeof(float),
                s => float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(float))
            },
            {
                typeof(int),
                s => int.TryParse(s,  NumberStyles.Any,CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(int))
            },
            {
                typeof(long),
                s => long.TryParse(s,  NumberStyles.Any,CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(long))
            },
            {
                typeof(short),
                s => short.TryParse(s,  NumberStyles.Any,CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(short))
            },
#if !NETSTANDARD
            {
                typeof(TimeOnly),
                s => TimeOnly.TryParse(s, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : throw new InvalidConstantStringToTypeConversionException(s, typeof(TimeOnly))
            }
#endif
        };

    public bool CanConvert(object? value, Type destination) =>
        value switch
        {
            null => true,
            string => destination.IsNullable()
                      && _stringParseMethods.ContainsKey(destination.GetNullableArgument())
                      || _stringParseMethods.ContainsKey(destination),
            _ => false
        };

    public object? ConvertTo(object? value, Type destination) =>
        value switch
        {
            null => destination.IsNullable() ? null : Activator.CreateInstance(destination),
            string str when destination.IsNullable() &&
                            (string.IsNullOrWhiteSpace(str) || str == nullKeyword.Value) => null,
            string str => _stringParseMethods[destination.GetNullableArgument()](str),
            _ => throw new InvalidConstantStringToTypeConversionException(value, destination)
        };
}