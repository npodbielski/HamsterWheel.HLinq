using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public class ToInterfaceConverter : IConfigurableValueConverter
{
    public int Priority => 1005;

    public (bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null) =>
        //if object implements interface, just return it
        to.IsInterface && value?.GetType().GetInterface(to.Name) is not null
            ? (true, value)
            : (false, null);
}