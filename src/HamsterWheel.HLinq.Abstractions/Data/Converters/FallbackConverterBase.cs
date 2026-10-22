using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public abstract class FallbackConverterBase : IFallbackConverter
{
    public int Priority => 1999;

    public abstract (bool success, object? result) TryConvert(object? value, Type to,
        CultureInfo? culture = null);
}
