namespace HamsterWheel.HLinq.Data.Converters;

public sealed class EnumValueConverter : BaseConfigurableValueConverter
{
    public override int Priority => 1003;
    public override bool CanConvert(object? value, Type destination) => destination.IsEnum;

    public override object ConvertTo(object? value, Type destination) =>
        (value is string str && Enum.TryParse(destination, str, true, out var val))
            ? val
            : base.ConvertTo(value, destination)!;
}

public sealed class NullableEnumValueConverter(INullKeyword nullKeyword, EnumValueConverter baseConverter)
    : BaseConfigurableValueConverter
{
    public override int Priority => 1003;

    public override bool CanConvert(object? value, Type destination) =>
        destination.IsGenericType
        && destination.GetGenericTypeDefinition() == typeof(Nullable<>)
        && destination.GetGenericArguments().First().IsEnum;

    public override object? ConvertTo(object? value, Type destination) =>
        value is string str && str == nullKeyword.Null
            ? null
            : baseConverter.ConvertTo(value, destination.GetGenericArguments().First());
}