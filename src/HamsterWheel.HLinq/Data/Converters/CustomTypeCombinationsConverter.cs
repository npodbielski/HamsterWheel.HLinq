using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public class CustomTypeCombinationsConverter : IConfigurableValueConverter
{
    public int Priority => 1004;

    private static readonly Dictionary<(Type, Type), Func<object, object>> CustomConversions =
        new()
        {
#if !NETSTANDARD
            {
                (typeof(DateTimeOffset), typeof(TimeOnly)), v =>
                {
                    var timeSpan = ((DateTimeOffset)v).ToUniversalTime().TimeOfDay;
                    return new TimeOnly(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
            },
            {
                (typeof(DateTime), typeof(TimeOnly)), v =>
                {
                    var timeSpan = ((DateTime)v).ToUniversalTime().TimeOfDay;
                    return new TimeOnly(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                }
            }
#endif
        };

    public (bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null) =>
        value is not null && CustomConversions.TryGetValue((value.GetType(), to), out var convert)
            ? (true, convert(value))
            : (false, null);
}