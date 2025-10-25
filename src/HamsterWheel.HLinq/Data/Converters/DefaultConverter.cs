using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace HamsterWheel.HLinq.Data.Converters;

public class DefaultConverter(IFallbackConverter? fallbackSerializer = null) : IDefaultConverter
{
    internal static DefaultConverter Instance { get; } = new(); 
    
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

    private static readonly Dictionary<Type, Func<IFormattable, object>> FormattableMethods =
        new()
        {
            { typeof(DateTimeOffset), c => c.ToString("O", CultureInfo.InvariantCulture) },
            { typeof(DateTime), c => c.ToString("O", CultureInfo.InvariantCulture) }
        };

    [return: NotNullIfNotNull(nameof(value))]
    public T? ConvertTo<T>(object? value)
    {
        if (value is T typedValue)
        {
            return typedValue;
        }

        return (T?)ConvertTo(typeof(T), value);
    }

    [return: NotNullIfNotNull(nameof(value))]
    public object? ConvertTo(Type targetType, object? value)
    {
        if (targetType == typeof(bool))
        {
            return ConvertToBoolean(value);
        }

        switch (value)
        {
            case IFormattable formattable when FormattableMethods.TryGetValue(value.GetType(), out var formattableFunc):
                return formattableFunc(formattable);
            case IConvertible convertible when ConvertibleMethods.TryGetValue(targetType, out var convertibleFunc):
                return convertibleFunc(convertible);
        }

        if (targetType == typeof(DateTimeOffset) && value is string stringValue)
        {
            return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }

        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value?.ToString() ?? "0", true);
        }

        if (targetType.IsInterface && value?.GetType().GetInterface(targetType.Name) is not null)
        {
            return value;
        }

        return fallbackSerializer?.ConvertTo(targetType, value) is (true, var converted)
            ? converted
            : ConvertViaSerialization(value, targetType);
    }

    public bool ConvertToBoolean(object? arg)
    {
        if (arg is null)
        {
            return false;
        }

        if (arg is bool b)
        {
            return b;
        }

        if (arg is char c)
        {
            if (char.IsWhiteSpace(c) || c == '\0')
            {
                return false;
            }

            arg = c.ToString();
        }

        if (arg is DateTime dt)
        {
            return dt != DateTime.MinValue && dt != DateTime.MaxValue;
        }

        if (arg is DateTimeOffset dto)
        {
            return dto != DateTimeOffset.MinValue && dto != DateTimeOffset.MaxValue;
        }

#if !NETSTANDARD
        if (arg is TimeOnly to)
        {
            return to != TimeOnly.MinValue && to != TimeOnly.MaxValue;
        }
#endif

        if (arg is IConvertible convertible)
        {
            try
            {
                return convertible.ToBoolean(CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                //swallow and continue with conversion
            }
        }

        if (arg is string str)
        {
            str = str.ToLower(CultureInfo.InvariantCulture);
            if (str is "0" or "no" or "f" or "n" or "\0")
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(str);
        }

        //at this point it should be not null custom object type -> return true
        return true;
    }

    private static object? ConvertViaSerialization(object? o, Type targetType)
    {
        string jsonString;
        if (o is string str)
        {
            jsonString = str;
        }
        else
        {
            jsonString = JsonSerializer.Serialize(o);
        }

        return JsonSerializer.Deserialize(jsonString, targetType);
    }
}