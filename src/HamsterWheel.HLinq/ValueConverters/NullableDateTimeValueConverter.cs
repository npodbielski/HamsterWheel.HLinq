using System.Globalization;

namespace HamsterWheel.HLinq.ValueConverters;

public sealed class DateTimeOffsetValueConverter : BaseValueConverter<DateTimeOffset>
{
    public override object Convert(string stringValue)
    {
        return DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, out var number)
            ? number
            : base.Convert(stringValue)!;
    }
}

public sealed class NullableDateTimeOffsetValueConverter(DateTimeOffsetValueConverter baseConverter) :
    BaseValueConverter<DateTimeOffset?>
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