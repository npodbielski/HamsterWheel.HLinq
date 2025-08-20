namespace HamsterWheel.HLinq.Data.Converters;

public interface IFallbackConverter
{
    (bool, object?) ConvertTo(Type targetType, object? value);
}