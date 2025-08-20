using System.Globalization;

namespace HamsterWheel.HLinq.ValueConverters;

public sealed class DoubleValueConverter : BaseValueConverter<double>
{
    public override object Convert(string stringValue)
    {
        return double.TryParse(stringValue, CultureInfo.InvariantCulture, out var number)
            ? number
            : base.Convert(stringValue)!;
    }
}

public sealed class NullableDoubleValueConverter(DoubleValueConverter baseConverter) :
    BaseValueConverter<double?>
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