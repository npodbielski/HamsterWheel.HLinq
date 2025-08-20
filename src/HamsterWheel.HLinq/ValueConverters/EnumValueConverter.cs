namespace HamsterWheel.HLinq.ValueConverters;

public sealed class EnumValueConverter : BaseConfigurableValueConverter
{
    public override bool CanConvert(Type destination) => destination.IsEnum;

    public override object ConvertTo(string stringValue, Type destination) =>
        Enum.TryParse(destination, stringValue, true, out var val)
            ? val
            : base.ConvertTo(stringValue, destination)!;
}

public sealed class NullableEnumValueConverter(EnumValueConverter baseConverter) : BaseConfigurableValueConverter
{
    public override bool CanConvert(Type destination)
    {
        return destination.IsGenericType 
               && destination.GetGenericTypeDefinition() == typeof(Nullable<>)
               && destination.GetGenericArguments().First().IsEnum;
    }

    public override object? ConvertTo(string stringValue, Type destination)
    {
        return stringValue switch
        {
            "null" => null,
            _ => baseConverter.ConvertTo(stringValue, destination.GetGenericArguments().First())
        };
    }
}