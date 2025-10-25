using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.ValueConverters;

public abstract class BaseValueConverter<T> : IValueConverter
{
    public Type For() => typeof(T);

    public virtual object? Convert(string value) => throw new InvalidConstantStringToTypeConversionException(value, For());
}