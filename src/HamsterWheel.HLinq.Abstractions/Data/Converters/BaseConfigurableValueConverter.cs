using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

public abstract class BaseConfigurableValueConverter : IConfigurableValueConverter
{
    public abstract int Priority { get; }
    public abstract bool CanConvert(object? value, Type destination);

    public virtual object? ConvertTo(object? value, Type destination) =>
        throw new InvalidConstantStringToTypeConversionException(value, destination);
}