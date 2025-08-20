using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.ValueConverters;

public abstract class BaseConfigurableValueConverter : IConfigurableValueConverter
{
    public abstract bool CanConvert(Type destination);

    public virtual object? ConvertTo(string value, Type destination) =>
        throw new InvalidConstantStringToTypeConversionException(value, destination);
}