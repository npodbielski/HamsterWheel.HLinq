using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public class ToInterfaceConverter : IConfigurableValueConverter
{
    public int Priority => 1005;

    public (bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null) =>
        to.IsInterface && value is not null && to.IsAssignableFrom(value.GetType())
            ? (true, value)
            : (false, null);
}