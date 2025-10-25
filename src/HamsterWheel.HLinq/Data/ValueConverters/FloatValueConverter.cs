using System.Globalization;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Data.ValueConverters;

public sealed class FloatValueConverter : BaseValueConverter<float>
{
    public override object Convert(string stringValue) =>
        float.TryParse(stringValue, CultureInfo.InvariantCulture, out var number)
            ? number
            : base.Convert(stringValue)!;
}

public sealed class NullableFloatValueConverter(FloatValueConverter baseConverter) :
    BaseValueConverter<float?>
{
    public override object? Convert(string stringValue) =>
        stringValue switch
        {
            "null" => null,
            _ => baseConverter.Convert(stringValue)
        };
}