namespace HamsterWheel.HLinq.ValueConverters;

public sealed class ByteValueConverter : BaseValueConverter<byte>
{
    public override object Convert(string stringValue) =>
        byte.TryParse(stringValue, out var @int) ? @int : base.Convert(stringValue)!;
}

public sealed class NullableByteValueConverter(ByteValueConverter baseConverter) :
    BaseValueConverter<byte?>
{
    public override object? Convert(string stringValue) =>
        stringValue switch
        {
            "null" => null,
            _ => baseConverter.Convert(stringValue)
        };
}