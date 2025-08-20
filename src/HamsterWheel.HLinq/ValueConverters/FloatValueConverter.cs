using System.Globalization;

namespace HamsterWheel.HLinq.ValueConverters;

public sealed class FloatValueConverter : BaseValueConverter<float>
{
    public override object Convert(string stringValue)
    {
        return float.TryParse(stringValue, CultureInfo.InvariantCulture, out var number)
            ? number
            : base.Convert(stringValue)!;
    }
}

public sealed class NullableFloatValueConverter(FloatValueConverter baseConverter) :
    BaseValueConverter<float?>
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