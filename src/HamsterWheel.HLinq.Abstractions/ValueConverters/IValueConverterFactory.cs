namespace HamsterWheel.HLinq.ValueConverters;

public interface IValueConverterFactory
{
    IValueConverter GetConverterFor(Type type);
}