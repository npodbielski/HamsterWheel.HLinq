namespace HamsterWheel.HLinq.ValueConverters;

public sealed class GuidValueConverter : BaseValueConverter<Guid>
{
    public override object Convert(string stringValue) => 
        Guid.TryParse(stringValue, out var guid) ? guid : base.Convert(stringValue)!;
}