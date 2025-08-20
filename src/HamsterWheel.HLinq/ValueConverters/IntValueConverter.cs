namespace HamsterWheel.HLinq.ValueConverters;

public sealed class IntValueConverter : BaseValueConverter<int>
{
    public override object Convert(string stringValue)
    {
        return int.TryParse(stringValue, out var @int) ? @int : base.Convert(stringValue)!;
    }
}

public sealed class NullableIntValueConverter(IntValueConverter baseConverter) :
    BaseValueConverter<int?>
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