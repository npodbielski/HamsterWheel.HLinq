namespace HamsterWheel.HLinq.ValueConverters;

public sealed class LongValueConverter : BaseValueConverter<long>
{
    public override object Convert(string stringValue)
    {
        return long.TryParse(stringValue, out var @long) ? @long : base.Convert(stringValue)!;
    }
}

public sealed class NullableLongValueConverter(LongValueConverter baseConverter) :
    BaseValueConverter<long?>
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