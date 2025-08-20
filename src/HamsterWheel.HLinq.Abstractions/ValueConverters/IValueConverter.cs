namespace HamsterWheel.HLinq.ValueConverters;

public interface IValueConverter
{
    Type For();
    object? Convert(string value);
}