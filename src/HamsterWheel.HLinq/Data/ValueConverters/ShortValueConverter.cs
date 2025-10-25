namespace HamsterWheel.HLinq.Data.ValueConverters;

public sealed class ShortValueConverter : BaseValueConverter<short>
{
    public override object Convert(string stringValue) =>
        short.TryParse(stringValue, out var @int) ? @int : base.Convert(stringValue)!;
}

public sealed class NullableShortValueConverter(ShortValueConverter baseConverter) :
    BaseValueConverter<short?>
{
    public override object? Convert(string stringValue) =>
        stringValue switch
        {
            "null" => null,
            _ => baseConverter.Convert(stringValue)
        };
}