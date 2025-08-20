using System.Globalization;

namespace HamsterWheel.HLinq.ValueConverters;

public sealed class TimeValueConverter : BaseValueConverter<TimeOnly>
{
    public override object Convert(string stringValue)
    {
        return TimeOnly.TryParse(stringValue, CultureInfo.InvariantCulture, out var number)
            ? number
            : base.Convert(stringValue)!;
    }
}

public sealed class NullableTimeValueConverter(TimeValueConverter baseConverter) :
    BaseValueConverter<TimeOnly?>
{
    public override object? Convert(string stringValue)
    {
        return stringValue switch
        {
            "null" => null,
            _ => baseConverter.Convert(stringValue)
        };
    }
}