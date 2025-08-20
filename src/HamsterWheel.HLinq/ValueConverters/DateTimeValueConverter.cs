using System.Globalization;

namespace HamsterWheel.HLinq.ValueConverters;

public sealed class DateTimeValueConverter : BaseValueConverter<DateTime>
{
    public override object Convert(string stringValue)
    {
        return DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, out var number)
            ? number
            : base.Convert(stringValue)!;
    }
}

public sealed class NullableDateTimeValueConverter(DateTimeValueConverter baseConverter) :
    BaseValueConverter<DateTime?>
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