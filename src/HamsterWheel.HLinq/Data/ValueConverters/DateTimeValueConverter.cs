using System.Globalization;

namespace HamsterWheel.HLinq.Data.ValueConverters;

public sealed class DateTimeValueConverter : BaseValueConverter<DateTime>
{
    public override object Convert(string stringValue) =>
        DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, out var number)
            ? number
            : base.Convert(stringValue)!;
}

public sealed class NullableDateTimeValueConverter(DateTimeValueConverter baseConverter) :
    BaseValueConverter<DateTime?>
{
    public override object? Convert(string stringValue) =>
        stringValue switch
        {
            "null" => null,
            _ => baseConverter.Convert(stringValue)
        };
}