namespace HamsterWheel.HLinq.ValueConverters;

public sealed class BoolValueConverter : BaseValueConverter<bool>
{
    public override object Convert(string stringValue)
    {
        return stringValue switch
        {
            "true" or "True" => true,
            "false" or "False" => false,
            _ => base.Convert(stringValue)!
        };
    }
}

public sealed class NullableBoolValueConverter(BoolValueConverter boolConverter) :
    BaseValueConverter<bool?>
{
    public override object? Convert(string stringValue)
    {
        return stringValue switch
        {
            "null" => null,
            _ => boolConverter.Convert(stringValue)
        };
    }
}