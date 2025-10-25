namespace HamsterWheel.HLinq.Data.ValueConverters;

public interface IValueConverter
{
    Type For();
    object? Convert(string value);
}