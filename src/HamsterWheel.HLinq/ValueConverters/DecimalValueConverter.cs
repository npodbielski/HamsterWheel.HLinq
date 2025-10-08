namespace HamsterWheel.HLinq.ValueConverters;

public sealed class DecimalValueConverter : BaseValueConverter<decimal>
{
    public override object Convert(string stringValue) =>
        decimal.TryParse(stringValue, out var number) ? number : base.Convert(stringValue)!;
}

public sealed class NullableDecimalValueConverter(DecimalValueConverter baseConverter) :
    BaseValueConverter<decimal?>
{
    public override object? Convert(string stringValue) =>
        stringValue switch
        {
            "null" => null,
            _ => baseConverter.Convert(stringValue)
        };
}