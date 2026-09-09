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

    public bool CanConvert(object? value, Type to) =>
        value is not null && CustomConversions.ContainsKey((value.GetType(), to));

    public object? ConvertTo(object? value, Type to) => CustomConversions[(value!.GetType(), to)](value);
}