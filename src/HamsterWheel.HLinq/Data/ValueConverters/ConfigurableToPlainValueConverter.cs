using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Data.ValueConverters;

public sealed class ConfigurableToPlainValueConverter(Type destination, IConfigurableValueConverter converter)
    : IValueConverter
{
    public Type For() => destination;

    public object? Convert(string value) => converter.ConvertTo(value, destination);
}