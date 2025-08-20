namespace HamsterWheel.HLinq.ValueConverters;

public interface IConfigurableValueConverter
{
    bool CanConvert(Type destination);
    object? ConvertTo(string stringValue, Type destination);
}