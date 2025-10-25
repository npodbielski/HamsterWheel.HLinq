namespace HamsterWheel.HLinq.Data.ValueConverters;

public interface IConfigurableValueConverter
{
    bool CanConvert(Type destination);
    object? ConvertTo(string stringValue, Type destination);
}