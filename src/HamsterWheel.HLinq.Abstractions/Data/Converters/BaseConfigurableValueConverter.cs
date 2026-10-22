using System.Globalization;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

public abstract class BaseConfigurableValueConverter : IConfigurableValueConverter
{
    public abstract int Priority { get; }

    /// <inheritdoc/>
    public abstract (bool success, object? result) TryConvert(object? value, Type to,
        CultureInfo? culture = null);

    protected static InvalidConstantStringToTypeConversionException CannotConvert(object? value, Type to) =>
        new(value, to);
}