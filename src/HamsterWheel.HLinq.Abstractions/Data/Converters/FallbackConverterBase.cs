namespace HamsterWheel.HLinq.Data.Converters;

public abstract class FallbackConverterBase : IFallbackConverter
{
    public int Priority => 1999;
    public virtual bool CanConvert(object? value, Type to) => true;

    public abstract object? ConvertTo(object? value, Type to);
}