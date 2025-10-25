using System.Globalization;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Data.ValueConverters;

public sealed class DoubleValueConverter : BaseValueConverter<double>
{
    public override object Convert(string stringValue) =>
        double.TryParse(stringValue, CultureInfo.InvariantCulture, out var number)
            ? number
            : base.Convert(stringValue)!;
}

public sealed class NullableDoubleValueConverter(DoubleValueConverter baseConverter) :
    BaseValueConverter<double?>
{
    public override object? Convert(string stringValue) =>
        stringValue switch
        {
            "null" => null,
            _ => baseConverter.Convert(stringValue)
        };
}