namespace HamsterWheel.HLinq.Data.ValueConverters;

public interface IValueConverterFactory
{
    IValueConverter GetConverterFor(Type type);
}