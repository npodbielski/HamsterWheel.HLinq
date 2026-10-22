using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public sealed class EnumValueConverter : BaseConfigurableValueConverter
{
    public override int Priority => 1003;

    public override (bool success, object? result) TryConvert(object? value, Type destination,
        CultureInfo? culture = null)
    {
        if (!destination.IsEnum)
        {
            return (false, null);
        }

        if (IsIntegral(value))
        {
            return (true, NumberToEnum(value!, destination));
        }

        //this type is an enum so value must be converted or exception must be thrown if value is
        //invalid. Conversion cannot fallback to next converter.
        return value?.ToString() is { } text &&
               text.TryParseEnum(destination) is { convertible: true, enumValue: { } val }
            ? (true, val)
            : throw CannotConvert(value, destination);
    }

    private static bool IsIntegral(object? value) =>
        value switch
        {
            //take only integral part of the fraction for the enum conversion process
            byte or sbyte or short or ushort or int or uint or long or ulong => true,
            double d => !double.IsNaN(d) && !double.IsInfinity(d) && d % 1 == 0,
            float f => !float.IsNaN(f) && !float.IsInfinity(f) && f % 1 == 0,
            decimal m => m == decimal.Truncate(m),
            _ => false
        };

    private static object NumberToEnum(object value, Type destination)
    {
        try
        {
            return Enum.ToObject(destination,
                Convert.ChangeType(value, Enum.GetUnderlyingType(destination), CultureInfo.InvariantCulture));
        }
        catch (Exception e) when (e is InvalidCastException or OverflowException or FormatException)
        {
            throw CannotConvert(value, destination);
        }
    }
}

public sealed class NullableEnumValueConverter(INullKeyword nullKeyword, EnumValueConverter baseConverter)
    : BaseConfigurableValueConverter
{
    public override int Priority => 1003;

    public override (bool success, object? result) TryConvert(object? value, Type destination,
        CultureInfo? culture = null)
    {
        if (!destination.IsGenericType
            || destination.GetGenericTypeDefinition() != typeof(Nullable<>)
            || !destination.GetGenericArguments().First().IsEnum)
        {
            return (false, null);
        }

        if (value is string text && text == nullKeyword.Value)
        {
            return (true, null);
        }

        //get enum type and use base, non-nullable converter
        return baseConverter.TryConvert(value, destination.GetNullableArgument(), culture);
    }
}
