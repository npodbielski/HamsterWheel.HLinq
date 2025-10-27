namespace HamsterWheel.HLinq.Data.Converters;

public interface IConfigurableValueConverter
{
    int Priority { get; }
    bool CanConvert(object? value, Type to);
    object? ConvertTo(object? value, Type to);
}