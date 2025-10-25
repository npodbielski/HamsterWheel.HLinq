namespace HamsterWheel.HLinq.Data.ValueConverters;

public sealed class LongValueConverter : BaseValueConverter<long>
{
    public override object Convert(string stringValue) =>
        long.TryParse(stringValue, out var @long) ? @long : base.Convert(stringValue)!;
}

public sealed class NullableLongValueConverter(LongValueConverter baseConverter) :
    BaseValueConverter<long?>
{
    public override object? Convert(string stringValue) =>
        stringValue switch
        {
            "null" => null,
            _ => baseConverter.Convert(stringValue)
        };
}