namespace HamsterWheel.HLinq.Data.Converters;

public interface IDefaultConverter
{
    T? ConvertTo<T>(object? value);
    object? ConvertTo(Type targetType, object? value);
    bool ConvertToBoolean(object? arg);
}