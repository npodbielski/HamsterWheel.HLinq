using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Data.ValueConverters;

public sealed class GuidValueConverter : BaseValueConverter<Guid>
{
    public override object Convert(string stringValue)
    {
        if (stringValue.IsDoubleQuoted())
        {
            stringValue = stringValue.UnQuote();
        }

        return Guid.TryParse(stringValue, out var guid) ? guid : base.Convert(stringValue)!;
    }
}